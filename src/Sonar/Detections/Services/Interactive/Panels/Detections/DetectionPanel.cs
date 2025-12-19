using Humanizer;
using Sonar.Detections.Extensions;
using Sonar.Metrics.Services;
using Sonar.Rules;
using Sonar.Rules.Extensions;
using Sonar.Rules.Stores;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Detections;

internal sealed class DetectionPanel(IRuleStore ruleStore, TaskCompletionSource monitor, int capacity) : InteractivePanel<IOrderedEnumerable<RuleMatch>>
{
    private readonly Table detectionTable = new Table().Expand().NoBorder().Caption($"Most {capacity} recent detections");
    private readonly BreakdownChart ruleBreakdownChart = new BreakdownChart().FullSize().ShowTagValues();
    private readonly BreakdownChart eventBreakdownChart = new BreakdownChart().FullSize().ShowTagValues();
    private readonly BreakdownChart detectionBreakdownChart = new BreakdownChart().FullSize().ShowTagValues();
    private readonly BreakdownChart severityBreakdownChart = new BreakdownChart().FullSize().ShowTagValues();
    private const int MaxDetailsWidth = 75;

    public static ValueTask BuildAsync(IRuleStore ruleStore, TaskCompletionSource monitor, int limit, Func<CancellationToken, ValueTask<IOrderedEnumerable<RuleMatch>>> factory, CancellationToken cancellationToken)
    {
        var panel = new DetectionPanel(ruleStore, monitor, limit);
        return panel.BuildAsync(factory, cancellationToken);
    }

    public override async ValueTask<IRenderable> BuildLayoutAsync(IOrderedEnumerable<RuleMatch> value, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[red]Detections[/] ([italic]Monitoring... Press H for help[/])").LeftJustified());
        await WaitAsync(cancellationToken);

        var layout = new Layout("Root")
            .SplitRows(new Layout("Header")
                    .SplitColumns(new Layout("Rules"), new Layout("Events"), new Layout("Detections"), new Layout("Severity")),
                new Layout("Live").Ratio(5));

        detectionTable.AddColumn("[bold]Date[/]", c => c.Centered());
        detectionTable.AddColumn("[bold]Rule[/]", c => c.Centered());
        detectionTable.AddColumn("[bold]Severity[/]", c => c.Centered());
        detectionTable.AddColumn("[bold]Tactic[/]", c => c.Centered());
        detectionTable.AddColumn("[bold]Details[/]", c => c.LeftAligned());

        for (int i = 0; i < capacity; i++)
        {
            detectionTable.AddEmptyRow();
        }
        
        ruleBreakdownChart.AddItem("Enabled", MetricService.EnabledRuleCount, Color.Blue);
        ruleBreakdownChart.AddItem("Disabled", MetricService.RuleCount - MetricService.EnabledRuleCount, Color.Gray);
        eventBreakdownChart.AddItem("Total", MetricService.EventCount, Color.White);
        detectionBreakdownChart.AddItem("Total", MetricService.DetectionCount, Color.Red);
        foreach (var kvp in MetricService.DetectionCountBySeverity)
        {
            severityBreakdownChart.AddItem(Enum.GetName(kvp.Key)!, kvp.Value, kvp.Key.GetColor());
        }
        
        layout.GetLayout("Rules").Update(new Panel(ruleBreakdownChart).Expand().Header("Rules").RoundedBorder());
        layout.GetLayout("Events").Update(new Panel(eventBreakdownChart).Expand().Header("Events").RoundedBorder());
        layout.GetLayout("Detections").Update(new Panel(detectionBreakdownChart).Expand().Header("Detections").RoundedBorder());
        layout.GetLayout("Severity").Update(new Panel(severityBreakdownChart).Expand().Header("Severity").RoundedBorder());
        layout.GetLayout("Live").Update(new Panel(detectionTable).Expand().Header("Live").RoundedBorder());
        
        return layout;
    }

    public override void OnTick(IOrderedEnumerable<RuleMatch> rules)
    {
        var rowIndex = 0;
        eventBreakdownChart.Data.Clear();
        eventBreakdownChart.AddItem("Total", MetricService.EventCount, Color.White);
        detectionBreakdownChart.Data.Clear();
        detectionBreakdownChart.AddItem("Total", MetricService.DetectionCount, Color.Red);
        severityBreakdownChart.Data.Clear();
        foreach (var kvp in MetricService.DetectionCountBySeverity)
        {
            severityBreakdownChart.AddItem(Enum.GetName(kvp.Key)!, kvp.Value, kvp.Key.GetColor());
        }
        
        foreach (var match in rules)
        {
            detectionTable.UpdateCell(rowIndex, columnIndex: 0, new Text($"{match.Date.ToLocalTime():G}"));
            detectionTable.UpdateCell(rowIndex, columnIndex: 1, new Markup(AnsiConsole.Profile.Capabilities.Links ? $"[link={match.DetectionDetails.RuleMetadata.Link}]{match.DetectionDetails.RuleMetadata.Title}[/]": match.DetectionDetails.RuleMetadata.Title));
            detectionTable.UpdateCell(rowIndex, columnIndex: 2, new Markup($"[{GetColor(match)}]{Enum.GetName(match.DetectionDetails.RuleMetadata.Level.FromLevel())}[/]"));
            detectionTable.UpdateCell(rowIndex, columnIndex: 3, new Markup(ruleStore.TryGetTactic(match.DetectionDetails.RuleMetadata.Title, out var tactic, out var link) ? AnsiConsole.Profile.Capabilities.Links ? $"[link={link}]{tactic}[/]" : tactic : "N/A"));
            detectionTable.UpdateCell(rowIndex, columnIndex: 4, new Markup(FormatDetails(match)));
            rowIndex++;
        }
    }

    private Task WaitAsync(CancellationToken cancellationToken)
    {
        return monitor.Task.WaitAsync(cancellationToken);
    }

    private static string FormatDetails(RuleMatch match)
    {
        return string.Join($" {Constants.Separator} ", match.DetectionDetails.Details.Truncate(MaxDetailsWidth).Split(Constants.Separator, StringSplitOptions.RemoveEmptyEntries).Select(value => Format(value.Trim().Split(": ", StringSplitOptions.RemoveEmptyEntries))));
    }

    private static string Format(string[] pair)
    {
        return pair.Length == 2 ? $"[{Color.OrangeRed1}]{pair[0]}:[/] [{Color.LightSkyBlue1}]{pair[1]}[/]" : string.Empty;
    }

    private static string GetColor(RuleMatch match)
    {
        return match.DetectionDetails.RuleMetadata.Level.FromLevel().GetColor().ToString();
    }
}