using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Aggregations;

internal sealed class Aggregator(IAggregationRepository aggregationRepository, IPropertyStore rulePropertiesProvider, int maxEventsPerRule = 65536) : IAggregator, IPreAggregator
{
    private readonly ConcurrentDictionary<string, Lazy<EventLruTracker>> lruTrackers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ISet<string>> columnsByRuleId = new(StringComparer.OrdinalIgnoreCase);
    
    public WinEvent Matched(string ruleId, WinEvent match)
    {
        if (lruTrackers.TryGetValue(ruleId, out var cache))
        {
            cache.Value.Clear();
        }

        return match;
    }

    public bool ContainsColumn(string ruleId, string column) => columnsByRuleId.TryGetValue(ruleId, out var columns) && columns.Contains(column);
    
    public IEnumerable<WinEvent> Query(string ruleId, string query)
    {
        return aggregationRepository.Query(ruleId, query);
    }

    public Task TrimExpiredAsync(IDictionary<AggregationRule, IEnumerable<WinEvent>> aggregations, CancellationToken cancellationToken)
    {
        return Task.WhenAll(aggregations.Keys.Select(aggregationRule =>
        {
            if (lruTrackers.TryGetValue(aggregationRule.Id, out var cache))
            {
                cache.Value.TrimExpired();
                var deletedEventIds = cache.Value.GetDeletedEventIds();
                if (deletedEventIds.Count > 0)
                {
                    return aggregationRepository.DeleteAsync(aggregationRule.Id, deletedEventIds, cancellationToken);
                }
            }
            
            return Task.CompletedTask;
        }));
    }

    public Task AddAsync(IDictionary<AggregationRule, IEnumerable<WinEvent>> aggregations, CancellationToken cancellationToken)
    {
        return aggregationRepository.InsertAsync(aggregations, onInsert: OnWinEventInsert, rulePropertiesProvider, cancellationToken);
    }
    
    private void OnWinEventInsert(AggregationRule aggregationRule, long id, ISet<string> columns)
    {
        var cache = lruTrackers.GetOrAdd(aggregationRule.Id, valueFactory: _ => new Lazy<EventLruTracker>(() => new EventLruTracker(aggregationRule.CorrelationOrAggregationTimeSpan, maxEventsPerRule), LazyThreadSafetyMode.ExecutionAndPublication));
        cache.Value.OnWinEventInsert(id);
        
        columnsByRuleId.AddOrUpdate(aggregationRule.Id, addValueFactory: _ => columns, updateValueFactory:
            (_, current) =>
            {
                foreach (var column in columns)
                {
                    current.Add(column);
                }

                return current;
            });
    }

    [field: AllowNull, MaybeNull]
    public static Aggregator Instance
    {
        get => field ?? throw new NullReferenceException(nameof(Instance));
        set;
    }
}