using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Detections.Services;
using Sonar.Events.Services;

namespace Sonar;

internal sealed class SonarHostedService(ILogger<SonarHostedService> logger, IEventService eventService, IDetectionService detectionService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.WhenAll(eventService.ConsumeAsync(stoppingToken), detectionService.ProcessAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await detectionService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}