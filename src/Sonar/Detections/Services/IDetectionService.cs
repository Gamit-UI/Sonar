using Sonar.Rules;

namespace Sonar.Detections.Services;

internal interface IDetectionService : IDisposable
{
    void Push(RuleMatch match);
    Task ProcessAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}