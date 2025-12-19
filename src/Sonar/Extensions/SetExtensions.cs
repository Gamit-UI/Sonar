using ConcurrentCollections;

namespace Sonar.Extensions;

internal static class SetExtensions
{
    public static void AddRange<T>(this ConcurrentHashSet<T> targetHashSet, IEnumerable<T> collection)
    {
        foreach (var element in collection)
        {
            targetHashSet.Add(element);
        }
    }
}