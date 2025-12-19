using System.Diagnostics.Eventing.Reader;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Sonar.Events.Extensions;
using Sonar.Events.Pipelines;
using Sonar.Extensions;
using Sonar.Metrics.Services;
using Sonar.Rules.Serialization;

namespace Sonar.Events.Processors;

internal sealed class EventProcessor(ILogger<EventProcessor> logger, IEventLogPipeline<WinEvent> eventLogPipeline) : IEventProcessor
{
    private readonly IDictionary<string, IDisposable> subscriptions = new Dictionary<string, IDisposable>();
    
    public void Initialize()
    {
        foreach (var (name, channelType) in EnumerateChannels())
        {
            if (channelType is ChannelType.Debug or ChannelType.Analytic or ChannelType.Diagnostic) continue;
            if (subscriptions.ContainsKey(name)) continue;
            subscriptions.Add(name, CreateWatcher(name));
        }
    }
    
    private static IEnumerable<KeyValuePair<string, ChannelType>> EnumerateChannels()
    {
        const string channelsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels";
        using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var channelSubKey = localMachine.OpenSubKey(channelsKey, writable: false);
        if (channelSubKey == null) yield break;
        foreach (var channelName in channelSubKey.GetSubKeyNames())
        {
            using var channelNameSubKey = localMachine.OpenSubKey($@"{channelsKey}\{channelName}", writable: false);
            if (channelNameSubKey?.GetValue("Type") is not int type) continue;
            yield return new KeyValuePair<string, ChannelType>(channelName, (ChannelType)type);
        }

        yield return new KeyValuePair<string, ChannelType>("Application", ChannelType.Operational);
        yield return new KeyValuePair<string, ChannelType>("Security", ChannelType.Operational);
        yield return new KeyValuePair<string, ChannelType>("System", ChannelType.Operational);
    }
    
    private void OnCompleted()
    {
        logger.LogDebug("Completed observing events");
    }

    private void OnError(Exception error)
    {
        if (error is not OperationCanceledException)
        {
            logger.Throttle(nameof(EventProcessor), itself => itself.LogError(error, "An error has occurred"), expiration: TimeSpan.FromMinutes(1));
        }
    }

    private void OnNext(EventRecord eventRecord, Exception? exception)
    {
        if (exception is not null)
        {
            if (exception is OperationCanceledException) return;
            logger.Throttle(nameof(EventProcessor), itself => itself.LogError(exception, "An error has occurred"), expiration: TimeSpan.FromMinutes(1));
            return;
        }

        using (eventRecord)
        {
            if (!eventRecord.TryGetWinEvent(out var winEvent)) return;
            if (!eventLogPipeline.Push(winEvent))
            {
                logger.Throttle(eventRecord.ProviderName, itself => itself.LogError("An event has been lost because pipeline is full for provider {Provider}", eventRecord.ProviderName), expiration: TimeSpan.FromMinutes(1));
            }
            else
            {
                MetricService.AddEvent();
            }
        }
    }

    private IDisposable CreateWatcher(string channelName)
    {
        var eventWatcher = new EventLogWatcher(new EventLogQuery(channelName, PathType.LogName));
        eventWatcher.Enabled = true;
        var eventSubscription = Observable.FromEventPattern<EventRecordWrittenEventArgs>(h => eventWatcher.EventRecordWritten += h, h => eventWatcher.EventRecordWritten -= h).Subscribe(onNext: args => OnNext(args.EventArgs.EventRecord, args.EventArgs.EventException), onError: OnError, onCompleted: OnCompleted);
        return new CompositeDisposable(eventWatcher, eventSubscription);
    }

    public void Dispose()
    {
        foreach (var kvp in subscriptions)
        {
            kvp.Value.Dispose();
        }
        
        subscriptions.Clear();
    }
}