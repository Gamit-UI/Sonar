using Sonar.Rules.Serialization;

namespace Sonar.Rules.Aggregations.Interfaces;

internal interface IPreAggregator
{
    Task TrimExpiredAsync(IDictionary<AggregationRule, IEnumerable<WinEvent>> aggregations, CancellationToken cancellationToken);
    Task AddAsync(IDictionary<AggregationRule, IEnumerable<WinEvent>> aggregations, CancellationToken cancellationToken);
}