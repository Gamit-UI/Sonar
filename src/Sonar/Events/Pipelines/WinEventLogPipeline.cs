using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Sonar.Extensions;
using Sonar.Rules.Serialization;

namespace Sonar.Events.Pipelines;

internal sealed class WinEventLogPipeline : IEventLogPipeline<WinEvent>
{
    private readonly Channel<WinEvent> winEventChannel;

    public WinEventLogPipeline(ILogger<WinEventLogPipeline> logger)
    {
        var options = new BoundedChannelOptions(capacity: 1024 * Environment.ProcessorCount)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };

        winEventChannel = Channel.CreateBounded<WinEvent>(options, winEventLogDropped => logger.Throttle(winEventLogDropped.ProviderName, itself => itself.LogWarning("EventLog from provider {Provider} was dropped", winEventLogDropped.ProviderName), expiration: TimeSpan.FromMinutes(1)));
    }

    public bool Push(WinEvent winEvent)
    {
        return winEventChannel.Writer.TryWrite(winEvent);
    }

    public IAsyncEnumerable<WinEvent> ConsumeAsync(CancellationToken cancellationToken)
    {
        return winEventChannel.Reader.ReadAllAsync(cancellationToken);
    }
}