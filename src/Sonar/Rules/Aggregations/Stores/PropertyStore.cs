using System.Collections.Concurrent;
using ConcurrentCollections;
using Sonar.Extensions;
using Sonar.Rules.Aggregations.Interfaces;

namespace Sonar.Rules.Aggregations.Stores;

internal sealed class PropertyStore : IPropertyStore
{
    private readonly ConcurrentDictionary<string, ConcurrentHashSet<string>> propertiesByRuleId = new();
    private static readonly ConcurrentHashSet<string> Default = new();
    
    public void AddProperties(string ruleId, ISet<string> properties)
    {
        propertiesByRuleId.AddOrUpdate(ruleId, addValueFactory: _ =>
        {
            var set = new ConcurrentHashSet<string>();
            set.AddRange(properties);
            return set;
        }, updateValueFactory: (_, current) =>
        {
            current.AddRange(properties);
            return current;
        });
    }

    public ConcurrentHashSet<string> GetProperties(string ruleId)
    {
        return propertiesByRuleId.GetValueOrDefault(ruleId, Default);
    }
}