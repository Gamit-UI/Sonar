using Sonar.Detections.Extensions;
using Sonar.Rules.Stores;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Statistics.Rules;

internal sealed class RulePanel : InteractivePanel<IDictionary<string, Tuple<DetectionSeverity, long>>>
{
    private readonly Layout root;
    private readonly IRuleStore ruleStore;
    private readonly IDictionary<DetectionSeverity, Table> tables = new Dictionary<DetectionSeverity, Table>();
    private const int MaxRowsPerSeverity = 5;
    
    public RulePanel(Layout layout, IRuleStore ruleStore)
    {
        this.ruleStore = ruleStore;
        root = new Layout("Group")
            .SplitRows(
                new Layout("Top")
                    .SplitColumns(new Layout(Enum.GetName(DetectionSeverity.Critical)!), new Layout(Enum.GetName(DetectionSeverity.High)!), new Layout(Enum.GetName(DetectionSeverity.Medium)!)),
                new Layout("Bottom")
                    .SplitColumns(new Layout(Enum.GetName(DetectionSeverity.Low)!), new Layout(Enum.GetName(DetectionSeverity.Informational)!)));
        
        foreach (var severity in Enum.GetValues<DetectionSeverity>())
        {
            var name = Enum.GetName(severity)!;
            var table = new Table().Expand().NoBorder();
            table.AddColumn("Name", c => c.Centered());
            table.AddColumn("Count", c => c.LeftAligned());
            root.GetLayout(name).Update(new Panel(table).Expand().Header($"Top [{severity.GetColor()}]{name}[/] detections").RoundedBorder());
            tables.Add(severity, table);
        }
        
        layout.Update(root);
    }

    public override ValueTask<IRenderable> BuildLayoutAsync(IDictionary<string, Tuple<DetectionSeverity, long>> value, CancellationToken cancellationToken)
    {
        foreach (var group in value.GroupBy(kvp => kvp.Value.Item1))
        {
            if (tables.TryGetValue(group.Key, out var table))
            {
                foreach (var kvp in group.OrderByDescending(item => item.Value.Item2).Take(MaxRowsPerSeverity))
                {
                    table.AddRow(new Markup($"[{group.Key.GetColor()}]{kvp.Key}[/]"), new Markup($"[{group.Key.GetColor()}]{kvp.Value.Item2}[/]"));
                }
            }
        }

        return ValueTask.FromResult<IRenderable>(root);
    }
    
    public override void OnTick(IDictionary<string, Tuple<DetectionSeverity, long>> value)
    {
        foreach (var group in value.GroupBy(kvp => kvp.Value.Item1))
        {
            var rowIndex = 0;
            if (tables.TryGetValue(group.Key, out var table))
            {
                foreach (var kvp in group.OrderByDescending(item => item.Value.Item2).Take(MaxRowsPerSeverity))
                {
                    if (AnsiConsole.Profile.Capabilities.Links && ruleStore.TryGetLink(kvp.Key, out var link))
                    {
                        table.UpdateCell(rowIndex, columnIndex: 0, new Markup($"[{group.Key.GetColor()}][link={link}]{kvp.Key}[/][/]"));
                    }
                    else
                    {
                        table.UpdateCell(rowIndex, columnIndex: 0, new Markup($"[{group.Key.GetColor()}]{kvp.Key}[/]"));
                    }

                    table.UpdateCell(rowIndex, columnIndex: 1, new Markup($"[{group.Key.GetColor()}]{kvp.Value.Item2}[/]"));
                    rowIndex++;
                }
            }
        }
    }
}