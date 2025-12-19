using Humanizer;
using Sonar.Detections.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Statistics.Detections;

internal sealed class DetectionPanel(Layout layout) : InteractivePanel<IDictionary<DateTime, IDictionary<DetectionSeverity, long>>>
{
    private readonly Table table = new Table().MinimalBorder();

    public override ValueTask<IRenderable> BuildLayoutAsync(IDictionary<DateTime, IDictionary<DetectionSeverity, long>> value, CancellationToken cancellationToken)
    {
        foreach (var kvp in value)
        {
            table.AddColumn($"[bold]{kvp.Key.Humanize(dateToCompareAgainst: DateTime.Today)}[/]", c => c.Centered());
        }

        table.AddEmptyRow();

        layout.Update(new Panel(table).Expand().Header("Detection count by severity over time").RoundedBorder());
        return ValueTask.FromResult<IRenderable>(layout);
    }

    public override void OnTick(IDictionary<DateTime, IDictionary<DetectionSeverity, long>> value)
    {
        var columnIndex = 0;
        foreach (var kvp in value)
        {
            if (kvp.Value.Count == 0)
            {
                table.UpdateCell(0, columnIndex, new Markup("[italic]N/A[/]"));
                continue;
            }

            var barChart = new BarChart();
            foreach (var severity in kvp.Value)
            {
                barChart.AddItem(new string(Enum.GetName(severity.Key)!.Take(3).ToArray()), severity.Value, severity.Key.GetColor());
            }

            table.UpdateCell(0, columnIndex, barChart);
            columnIndex++;
        }
    }
}