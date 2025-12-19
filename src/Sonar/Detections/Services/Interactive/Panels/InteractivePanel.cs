using System.Reactive;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels;

internal abstract class InteractivePanel<T> : InteractivePanelBase<T>
{
    protected async ValueTask BuildAsync(Func<CancellationToken, ValueTask<T>> factory, CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(await BuildLayoutAsync(await factory(cancellationToken), cancellationToken))
            .Cropping(VerticalOverflowCropping.Bottom)
            .Overflow(VerticalOverflow.Ellipsis)
            .AutoClear(true)
            .StartAsync(async ctx => { await RefreshPeriodicallyAsync(ctx, await factory(cancellationToken), cancellationToken); });
    } 
    
    public abstract ValueTask<IRenderable> BuildLayoutAsync(T value, CancellationToken cancellationToken);
}

internal abstract class InteractivePanel : InteractivePanelBase<Unit>
{
    public async ValueTask BuildAsync(CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(await BuildLayoutAsync(cancellationToken))
            .Cropping(VerticalOverflowCropping.Bottom)
            .Overflow(VerticalOverflow.Ellipsis)
            .AutoClear(true)
            .StartAsync(async ctx => { await RefreshPeriodicallyAsync(ctx, Unit.Default, cancellationToken); });
    } 
    
    protected abstract ValueTask<IRenderable> BuildLayoutAsync(CancellationToken cancellationToken);
}

internal abstract class InteractivePanelBase<T>
{
    public abstract void OnTick(T value);
    
    protected async ValueTask RefreshPeriodicallyAsync(LiveDisplayContext context, T value, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                OnTick(value);
                context.Refresh();
            }
        }
        catch (OperationCanceledException)
        {
            // Suppress
        }
    }
}