using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using SharpHook;
using SharpHook.Data;
using Sonar.Databases.Repositories;
using Sonar.Detections.Services.Interactive.Panels.Detections;
using Sonar.Detections.Services.Interactive.Panels.Exports;
using Sonar.Detections.Services.Interactive.Panels.Help;
using Sonar.Detections.Services.Interactive.Panels.Statistics;
using Sonar.Helpers;
using Sonar.Rules;
using Sonar.Rules.Stores;

namespace Sonar.Detections.Services.Interactive;

internal sealed class InteractiveDetectionService : DetectionService
{
    private enum Panel
    {
        None,
        Detection,
        Statistic,
        Export,
        Help
    }
    
    private readonly ILogger<InteractiveDetectionService> logger;
    private readonly IDetectionRepository detectionRepository;
    private readonly IRuleStore ruleStore;
    private readonly IDisposable subscription;
    private readonly ConcurrentCircularBuffer<RuleMatch> detectionBuffer = new(MaxDetections);
    private readonly TaskCompletionSource detectionMonitor = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly EventLoopGlobalHook hook = new(GlobalHookType.Keyboard);
    private readonly Subject<KeyCode> keyCodeSubject = new();
    private readonly SemaphoreSlim semaphore = new(initialCount: 1, maxCount: 1);
    private const int MaxDetections = 25;
    private static readonly TimeSpan History = TimeSpan.FromDays(6);
    private Panel panel = Panel.None;
    
    public InteractiveDetectionService(ILogger<InteractiveDetectionService> logger, IDetectionRepository detectionRepository, IRuleStore ruleStore) : base(logger, detectionRepository)
    {
        this.logger = logger;
        this.detectionRepository = detectionRepository;
        this.ruleStore = ruleStore;
        subscription = new CompositeDisposable(
            SubscribeKeyCode(),
            Observable
                .FromEventPattern<KeyboardHookEventArgs>(h => hook.KeyPressed += h, h => hook.KeyPressed -= h)
                .Where(args =>
                {
                    if (panel == Panel.Export) return false;
                    if (args.EventArgs.Data.KeyCode is KeyCode.VcS && panel is not Panel.Statistic) return true;
                    if (args.EventArgs.Data.KeyCode is KeyCode.VcD && panel is not Panel.Detection) return true;
                    if (args.EventArgs.Data.KeyCode is KeyCode.VcE && panel is not Panel.Export) return true;
                    if (args.EventArgs.Data.KeyCode is KeyCode.VcH && panel is not Panel.Help) return true;
                    return false;
                })
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Do(arg => keyCodeSubject.OnNext(arg.EventArgs.Data.KeyCode))
                .Subscribe());
    }

    public override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        keyCodeSubject.OnNext(KeyCode.VcD);
        await Task.WhenAll(ConsumeAsync(cancellationToken), hook.RunAsync());
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        hook.Stop();
        return Task.CompletedTask;
    }

    private IDisposable SubscribeKeyCode()
    {
        return keyCodeSubject
            .Select(key =>
            {
                return Observable.FromAsync(async ct =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        if (key == KeyCode.VcD)
                        {
                            panel = Panel.Detection;
                            await DisplayDetectionsAsync(ct);
                        }
                        else if (key == KeyCode.VcS)
                        {
                            panel = Panel.Statistic;
                            await DisplayStatisticsAsync(ct);
                        }
                        else if (key == KeyCode.VcE)
                        {
                            panel = Panel.Export;
                            await DisplayExportsAsync(ct);
                            panel = Panel.None;
                            keyCodeSubject.OnNext(KeyCode.VcH);
                        }
                        else if (key == KeyCode.VcH)
                        {
                            panel = Panel.Help;
                            await DisplayHelpAsync(ct);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Suppress
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error has occurred");
                        panel = Panel.None;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            })
            .Switch()
            .Subscribe();
    }
    
    private ValueTask DisplayDetectionsAsync(CancellationToken cancellationToken)
    {
        return DetectionPanel.BuildAsync(ruleStore, detectionMonitor, MaxDetections, _ => ValueTask.FromResult(detectionBuffer.OrderByDescending(detection => detection.Date)), cancellationToken);
    }
    
    private async ValueTask DisplayStatisticsAsync(CancellationToken cancellationToken)
    {
        await new StatisticPanel(ruleStore, await detectionRepository.GetSeveritiesByDateAsync(History, cancellationToken), await detectionRepository.GetTopComputersAsync(limit: 3, cancellationToken), await detectionRepository.GetRulesAsync(cancellationToken)).BuildAsync(cancellationToken);
    }
    
    private async ValueTask DisplayExportsAsync(CancellationToken cancellationToken)
    {
        await new ExportPanel(ruleStore, detectionRepository).ExportAsync(cancellationToken);
    }
    
    private static ValueTask DisplayHelpAsync(CancellationToken cancellationToken)
    {
        return new HelpPanel().BuildAsync(cancellationToken);
    }

    protected override void OnMatch(RuleMatch match)
    {
        detectionBuffer.Enqueue(match);
        detectionMonitor.TrySetResult();
    }

    public override void Dispose()
    {
        keyCodeSubject.Dispose();
        subscription.Dispose();
        hook.Dispose();
    }
}