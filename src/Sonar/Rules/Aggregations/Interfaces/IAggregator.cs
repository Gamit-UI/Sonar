using Sonar.Rules.Serialization;

namespace Sonar.Rules.Aggregations.Interfaces;

internal interface IAggregator
{
    WinEvent Matched(string ruleId, WinEvent match);
    IEnumerable<WinEvent> Query(string ruleId, string query);
    bool ContainsColumn(string ruleId, string column);
}