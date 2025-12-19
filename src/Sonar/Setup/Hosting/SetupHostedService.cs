using Microsoft.Extensions.Hosting;
using Sonar.Databases.Repositories;
using Sonar.Events.Processors;
using Sonar.Rules.Aggregations;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Helpers;
using Sonar.Rules.Stores;
using Sonar.Setup.Services;
using Sonar.Setup.Services.Options;

namespace Sonar.Setup.Hosting;

internal abstract class SetupHostedService(ISetupService setupService, IDetectionRepository detectionRepository, IRuleStore ruleStore, IAggregationRepository aggregationRepository, IPropertyStore propertyStore, IEventProcessor eventProcessor) : IHostedService
{
    protected ValueTask InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        return detectionRepository.InitializeAsync(cancellationToken);
    }
    
    protected ValueTask<SetupOptions> InitializeSetupAsync(CancellationToken cancellationToken)
    {
        return setupService.InitializeAsync(cancellationToken);
    }

    protected void InitializeAggregator()
    {
        Aggregator.Instance = new Aggregator(aggregationRepository, propertyStore, maxEventsPerRule: 1024);
    }

    protected void InitializeEventProcessor()
    {
        eventProcessor.Initialize();
    }

    protected ValueTask LoadRuleConfigurationAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken)
    {
        return ConfigHelper.InitializeAsync(setupService, setupOptions, progress, cancellationToken);
    }

    protected ValueTask<int> LoadRulesAsync(SetupOptions setupOptions, IProgress<double> progress, Action onBeforeCount, Action<int> onCount, CancellationToken cancellationToken)
    {
        return ruleStore.InitializeAsync(setupOptions, progress, onBeforeCount, onCount, cancellationToken);
    }

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}