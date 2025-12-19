using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class Details(IDictionary<ProviderEventId, string> items)
{
    public IDictionary<ProviderEventId, string> Items { get; } = items;
}