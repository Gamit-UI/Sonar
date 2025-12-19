using Sonar.Events.Extensions;
using Sonar.Rules.Extensions;

namespace Sonar.Rules.Aggregations.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<string> ExtractProperties(this IEnumerable<string> properties)
    {
        foreach (var property in properties.Select(property => property.TakeLast(Constants.Dot)))
        {
            yield return property;
        }

        foreach (var property in WinEventExtensions.SystemColumns)
        {
            yield return property;
        }
    }
}