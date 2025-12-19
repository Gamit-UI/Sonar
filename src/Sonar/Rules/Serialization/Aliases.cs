using System.Text.Json.Serialization;
using Sonar.Rules.Helpers;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class Aliases(IDictionary<string, string> items)
{
    public IDictionary<string, string> Items { get; } = items;

    internal static readonly Lazy<Aliases> Instance = new(ConfigHelper.GetAliases, LazyThreadSafetyMode.ExecutionAndPublication);
}