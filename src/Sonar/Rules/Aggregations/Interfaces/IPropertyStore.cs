using ConcurrentCollections;

namespace Sonar.Rules.Aggregations.Interfaces;

internal interface IPropertyStore
{
    void AddProperties(string ruleId, ISet<string> properties);
    ConcurrentHashSet<string> GetProperties(string ruleId);
}