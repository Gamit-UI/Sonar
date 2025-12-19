using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class ChannelAbbrevations(IDictionary<string, string> items)
{
    public IDictionary<string, string> Items { get; } = items;
}