using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class TargetEventIds(ISet<int> items)
{
    public ISet<int> Items { get; } = items;
}