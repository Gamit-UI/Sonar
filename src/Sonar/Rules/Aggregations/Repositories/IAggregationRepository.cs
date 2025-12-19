using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Aggregations.Repositories;

internal interface IAggregationRepository
{
    Task InsertAsync(IDictionary<AggregationRule, IEnumerable<WinEvent>> aggregations, Action<AggregationRule, long, ISet<string>> onInsert, IPropertyStore rulePropertiesProvider, CancellationToken cancellationToken);
    IEnumerable<WinEvent> Query(string ruleId, string query);
    Task DeleteAsync(string ruleId, ISet<long> ids, CancellationToken cancellationToken);
}