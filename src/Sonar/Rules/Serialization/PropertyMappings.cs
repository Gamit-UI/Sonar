using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class PropertyMappings(IDictionary<ChannelEventId, PropertyMapping> items)
{
    public IDictionary<ChannelEventId, PropertyMapping> Items { get; } = items;
}