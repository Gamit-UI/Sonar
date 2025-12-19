using Sonar.Detections;
using Sonar.Rules;

namespace Sonar.Databases.Repositories;

internal interface IDetectionRepository : IAsyncDisposable
{
    ValueTask InitializeAsync(CancellationToken cancellationToken);
    ValueTask InsertAsync(RuleMatch match, CancellationToken cancellationToken);
    ValueTask<IDictionary<DateTime, IDictionary<DetectionSeverity, long>>> GetSeveritiesByDateAsync(TimeSpan since, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, IDictionary<DetectionSeverity, long>>> GetTopComputersAsync(int limit, CancellationToken cancellationToken);
    ValueTask<IDictionary<string, Tuple<DetectionSeverity, long>>> GetRulesAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<DetectionExport> EnumerateDetectionsByRuleTitleAsync(string rule, CancellationToken cancellationToken);
    IAsyncEnumerable<DetectionExport> EnumerateDetectionsAsync(IEnumerable<DetectionSeverity> severities, CancellationToken cancellationToken);
    IAsyncEnumerable<DetectionExport> EnumerateDetectionsByKeywordAsync(string keyword, CancellationToken cancellationToken);
}