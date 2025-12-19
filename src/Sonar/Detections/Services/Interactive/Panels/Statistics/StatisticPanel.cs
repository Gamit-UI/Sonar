using System.Reactive;
using Sonar.Detections.Services.Interactive.Panels.Statistics.Computers;
using Sonar.Detections.Services.Interactive.Panels.Statistics.Detections;
using Sonar.Detections.Services.Interactive.Panels.Statistics.Rules;
using Sonar.Rules.Stores;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Statistics;

internal sealed class StatisticPanel : InteractivePanel
{
    private readonly Layout layout;
    private readonly DetectionPanel detectionPanel;
    private readonly RulePanel rulePanel;
    
    private readonly IDictionary<ComputerPanel, IDictionary<DetectionSeverity, long>> computerPanels = new Dictionary<ComputerPanel, IDictionary<DetectionSeverity, long>>();
    private readonly IDictionary<DateTime, IDictionary<DetectionSeverity, long>> severityOverTime;
    private readonly IDictionary<string, Tuple<DetectionSeverity, long>> rules;

    public StatisticPanel(IRuleStore ruleStore, IDictionary<DateTime, IDictionary<DetectionSeverity, long>> severityOverTime, IDictionary<string, IDictionary<DetectionSeverity, long>> severityByComputer, IDictionary<string, Tuple<DetectionSeverity, long>> rules)
    {
        this.severityOverTime = severityOverTime;
        this.rules = rules;
        layout = new Layout("Statistics")
            .SplitRows(
                new Layout("Computer").Ratio(2).SplitColumns(severityByComputer.Select(kvp => new Layout(kvp.Key)).ToArray()),
                new Layout("Detection").Ratio(3),
                new Layout("Rule").Ratio(6));
        
        foreach (var kvp in severityByComputer)
        {
            computerPanels.Add(new ComputerPanel(layout.GetLayout(kvp.Key), kvp.Key), kvp.Value);
        }
        
        detectionPanel = new DetectionPanel(layout.GetLayout("Detection"));
        rulePanel = new RulePanel(layout.GetLayout("Rule"), ruleStore);
    }

    protected override async ValueTask<IRenderable> BuildLayoutAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[blue]Statistics[/] ([italic]Press H for help[/])").LeftJustified());
        foreach (var kvp in computerPanels)
        {
            await kvp.Key.BuildLayoutAsync(kvp.Value, cancellationToken);
        }
        
        await detectionPanel.BuildLayoutAsync(severityOverTime, cancellationToken);
        await rulePanel.BuildLayoutAsync(rules, cancellationToken);
        return layout;
    }

    public override void OnTick(Unit value)
    {
        foreach (var kvp in computerPanels)
        {
            kvp.Key.OnTick(kvp.Value);
        }

        detectionPanel.OnTick(severityOverTime);
        rulePanel.OnTick(rules);
    }
}