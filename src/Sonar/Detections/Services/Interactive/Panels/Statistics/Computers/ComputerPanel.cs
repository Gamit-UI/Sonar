using Sonar.Detections.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Statistics.Computers;

internal sealed class ComputerPanel(Layout layout, string computer) : InteractivePanel<IDictionary<DetectionSeverity, long>>
{
    private readonly BarChart chart = new();

    public override ValueTask<IRenderable> BuildLayoutAsync(IDictionary<DetectionSeverity, long> value, CancellationToken cancellationToken)
    {
        layout.Update(new Panel(chart).Expand().Header(computer).RoundedBorder());
        return ValueTask.FromResult<IRenderable>(layout);
    }

    public override void OnTick(IDictionary<DetectionSeverity, long> value)
    {
        chart.Data.Clear();
        foreach (var severity in Enum.GetValues<DetectionSeverity>())
        {
            chart.AddItem(Enum.GetName(severity)!, value.TryGetValue(severity, out var count) ? count : 0, severity.GetColor());
        }
    }
}