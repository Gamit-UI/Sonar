using System.Collections.Concurrent;
using Sonar.Detections;

namespace Sonar.Metrics.Services;

internal static class MetricService
{
    public static long RuleCount;
    public static long EnabledRuleCount;
    public static long EventCount;
    public static long DetectionCount => DetectionCountBySeverity.Sum(kvp => kvp.Value);
    public static readonly ConcurrentDictionary<DetectionSeverity, long> DetectionCountBySeverity = new();

    public static void AddEvent() => Interlocked.Increment(ref EventCount);
    public static void AddRule() => Interlocked.Increment(ref RuleCount);
    public static void AddEnabledRule() => Interlocked.Increment(ref EnabledRuleCount);
    public static void Add(DetectionSeverity severity) => DetectionCountBySeverity.AddOrUpdate(severity, addValue: 1, updateValueFactory: (_, current) => ++current);
}