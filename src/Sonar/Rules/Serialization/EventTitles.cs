using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class EventTitles(IDictionary<ChannelEventId, string> items)
{
    public IDictionary<ChannelEventId, string> Items { get; } = items;
}