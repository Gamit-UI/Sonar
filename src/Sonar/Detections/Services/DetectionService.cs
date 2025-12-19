using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Sonar.Databases.Repositories;
using Sonar.Extensions;
using Sonar.Metrics.Services;
using Sonar.Rules;
using Sonar.Rules.Extensions;

namespace Sonar.Detections.Services;

internal abstract class DetectionService : IDetectionService
{
    private readonly Channel<RuleMatch> ruleMatchChannel;
    private readonly IDetectionRepository detectionRepository;
    
    protected DetectionService(ILogger logger, IDetectionRepository detectionRepository)
    {
        var options = new BoundedChannelOptions(capacity: 1024 * Environment.ProcessorCount)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        ruleMatchChannel = Channel.CreateBounded<RuleMatch>(options, match => logger.Throttle(match.WinEvent.ProviderName, itself => itself.LogWarning("Rule match from provider {Provider} was dropped", match.WinEvent.ProviderName), expiration: TimeSpan.FromMinutes(1)));
        this.detectionRepository = detectionRepository;
    }

    public abstract Task ProcessAsync(CancellationToken cancellationToken);

    public abstract Task StopAsync(CancellationToken cancellationToken);
    
    protected async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await foreach (var match in ruleMatchChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await detectionRepository.InsertAsync(match, cancellationToken);
            OnMatch(match);
        }
    }

    protected abstract void OnMatch(RuleMatch match);
    
    public void Push(RuleMatch match)
    {
        if (ruleMatchChannel.Writer.TryWrite(match))
        {
            MetricService.Add(match.DetectionDetails.RuleMetadata.Level.FromLevel());
        }
    }

    public abstract void Dispose();
}