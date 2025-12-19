using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class ProvenRules(ISet<string> items)
{
    public ISet<string> Items { get; } = items;
}