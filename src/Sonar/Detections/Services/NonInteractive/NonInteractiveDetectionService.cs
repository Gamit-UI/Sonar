using Microsoft.Extensions.Logging;
using Sonar.Databases.Repositories;
using Sonar.Rules;

namespace Sonar.Detections.Services.NonInteractive;

internal sealed class NonInteractiveDetectionService(ILogger<NonInteractiveDetectionService> logger, IDetectionRepository detectionRepository) : DetectionService(logger, detectionRepository)
{
    public override Task ProcessAsync(CancellationToken cancellationToken)
    {
        return ConsumeAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override void OnMatch(RuleMatch match)
    {
        
    }

    public override void Dispose()
    {
        
    }
}