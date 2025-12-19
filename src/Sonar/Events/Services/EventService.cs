using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Detections.Services;
using Sonar.Events.Pipelines;
using Sonar.Helpers;
using Sonar.Rules;
using Sonar.Rules.Aggregations;
using Sonar.Rules.Serialization;
using Sonar.Rules.Stores;

namespace Sonar.Events.Services;

internal sealed class EventService : IEventService
{
    private readonly ILogger<EventService> logger;
    private readonly DataFlowHelper.PeriodicBlock<Tuple<WinEvent, AggregationRule>> aggregationBlock;
    private readonly IEventLogPipeline<WinEvent> eventLogPipeline;
    private readonly IRuleStore ruleStore;
    private readonly IDetectionService detectionService;
    private readonly IDisposable subscription;

    public EventService(ILogger<EventService> logger, IHostApplicationLifetime hostApplicationLifetime, IEventLogPipeline<WinEvent> eventLogPipeline, IRuleStore ruleStore, IDetectionService detectionService)
    {
        this.logger = logger;
        this.eventLogPipeline = eventLogPipeline;
        this.ruleStore = ruleStore;
        this.detectionService = detectionService;
        aggregationBlock = CreateAggregationBlock(hostApplicationLifetime.ApplicationStopping, out var disposableLink);
        subscription = disposableLink;
    }
    
    private DataFlowHelper.PeriodicBlock<Tuple<WinEvent, AggregationRule>> CreateAggregationBlock(CancellationToken cancellationToken, out IDisposable disposableLink)
    {
        var executionDataflow = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            SingleProducerConstrained = false,
            BoundedCapacity = 12,
            CancellationToken = cancellationToken
        };
        
        var periodicBlock = DataFlowHelper.CreatePeriodicBlock<Tuple<WinEvent, AggregationRule>>(TimeSpan.FromSeconds(5), count: 1000);
        var options = new DataflowLinkOptions { PropagateCompletion = true };
        var propagationBlock = new ActionBlock<IList<Tuple<WinEvent, AggregationRule>>>(async items => await ProcessAggregationsAsync(items, cancellationToken), executionDataflow);
        disposableLink = periodicBlock.LinkTo(propagationBlock, options);
        return periodicBlock;
    }
    
    private async Task ProcessAggregationsAsync(IList<Tuple<WinEvent, AggregationRule>> tuples, CancellationToken cancellationToken)
    {
        try
        {
            if (tuples.Count == 0) return;
            var winEventsByRule = tuples.GroupBy(tuple => tuple.Item2).ToDictionary(kvp => kvp.Key, kvp => kvp.Select(tuple => tuple.Item1));
            await Aggregator.Instance.TrimExpiredAsync(winEventsByRule, cancellationToken);
            await Aggregator.Instance.AddAsync(winEventsByRule, cancellationToken);
            await winEventsByRule.Keys.ProcessAllAsync(aggregationRule =>
            {
                if (aggregationRule.TryMatch(logger, out var match))
                {
                    OnMatch(match);
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await foreach (var winEvent in eventLogPipeline.ConsumeAsync(cancellationToken))
        {
            if (ruleStore.TryGetStandardRules(winEvent.EventId, out var standardRules))
            {
                foreach (var rule in standardRules)
                {
                    if (rule.TryMatch(logger, winEvent, out var match))
                    {
                        OnMatch(match);
                    }
                }
            }

            if (ruleStore.TryGetAggregationRules(winEvent.EventId, out var aggregationRules))
            {
                foreach (var rule in aggregationRules)
                {
                    if (rule.TryMatch(logger, winEvent))
                    {
                        await aggregationBlock.SendAsync(new Tuple<WinEvent, AggregationRule>(winEvent, rule), cancellationToken);
                    }
                }
            }
        }
    }
    
    private void OnMatch(RuleMatch ruleMatch)
    {
        detectionService.Push(ruleMatch);
    }

    public async ValueTask DisposeAsync()
    {
        await aggregationBlock.DisposeAsync();
        if (subscription is IAsyncDisposable subscriptionAsyncDisposable)
            await subscriptionAsyncDisposable.DisposeAsync();
        else
            subscription.Dispose();
    }
}