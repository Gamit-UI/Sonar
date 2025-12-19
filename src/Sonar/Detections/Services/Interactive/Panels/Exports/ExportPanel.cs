using System.Text.Json;
using Sonar.Databases.Repositories;
using Sonar.Extensions;
using Sonar.Helpers;
using Sonar.Rules.Stores;
using Spectre.Console;

namespace Sonar.Detections.Services.Interactive.Panels.Exports;

internal sealed class ExportPanel(IRuleStore ruleStore, IDetectionRepository detectionRepository)
{
    private enum Export
    {
        Rule,
        Severity,
        Keyword
    }

    private readonly string exportDirectory = Path.Join(AppContext.BaseDirectory, "Exports");

    public async ValueTask ExportAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[blue]Exports[/]").LeftJustified());
        var export = await AnsiConsole.PromptAsync(new SelectionPrompt<Export>()
            .UseConverter(value =>
            {
                return value switch
                {
                    Export.Rule => "Rule - Allows to query detections by rule name",
                    Export.Severity => "Severity - Allows to query detections by rule severity",
                    Export.Keyword => "Keyword - Allows to query detections by keyword",
                    _ => throw new ArgumentOutOfRangeException()
                };
            })
            .WrapAround()
            .Title("Select whether to export detections by rule name, severity, or keyword.")
            .AddChoices(Export.Rule, Export.Severity, Export.Keyword), cancellationToken);

        switch (export)
        {
            case Export.Rule:
                await ExportByRuleNameAsync(cancellationToken);
                break;
            case Export.Severity:
                await ExportByRuleSeverityAsync(cancellationToken);
                break;
            case Export.Keyword:
                await ExportByKeywordAsync(cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async ValueTask ExportByRuleNameAsync(CancellationToken cancellationToken)
    {
        var rule = await AnsiConsole.PromptAsync(new SelectionPrompt<string>()
            .Title("Select a [bold]rule name[/]:")
            .PageSize(5)
            .EnableSearch()
            .WrapAround()
            .MoreChoicesText("[grey](Move up and down to reveal more rules)[/]")
            .SearchPlaceholderText("Type a rule name...")
            .AddChoices(ruleStore.GetRuleTitles().Distinct().Order()), cancellationToken);
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }

        var exportPath = await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync("Exporting...", async _ =>
            {
                var exportPath = Path.Join(exportDirectory, $"Rule-{DateTime.Now:O}-{rule}.json".CleanFileName()).CleanFilePath();
                await using var stream = File.OpenWrite(exportPath);
                await JsonSerializer.SerializeAsync(stream, await detectionRepository.EnumerateDetectionsByRuleTitleAsync(rule, cancellationToken).ToListAsync(cancellationToken: cancellationToken), SerializationContext.Default.IEnumerableDetectionExport, cancellationToken: cancellationToken);
                return exportPath;
            });

        var path = new TextPath(exportPath)
            .LeafColor(Color.Green);
        var panel = new Panel(path);
        panel.Header($"[white on darkgreen]{Path.GetFileNameWithoutExtension(exportPath)}[/]").HeavyBorder();
        AnsiConsole.Write(panel);
    }

    private async ValueTask ExportByRuleSeverityAsync(CancellationToken cancellationToken)
    {
        var severities = await AnsiConsole.PromptAsync(new MultiSelectionPrompt<DetectionSeverity>()
            .Title("Select [bold]severities[/]:")
            .PageSize(5)
            .Required()
            .WrapAround()
            .MoreChoicesText("[grey](Move up and down to reveal more severities)[/]")
            .UseConverter(value => Enum.GetName(value)!)
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle a severity, " +
                "[green]<enter>[/] to accept)[/]")
            .AddChoices(DetectionSeverity.Informational, DetectionSeverity.Low, DetectionSeverity.Medium, DetectionSeverity.High, DetectionSeverity.Critical), cancellationToken);

        var exportPath = await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync("Exporting...", async _ =>
            {
                var exportPath = Path.Join(exportDirectory, $"Severities-{DateTime.Now:O}-{string.Join("-", severities)}.json".CleanFileName()).CleanFilePath();
                await using var stream = File.OpenWrite(exportPath);
                await JsonSerializer.SerializeAsync(stream, await detectionRepository.EnumerateDetectionsAsync(severities, cancellationToken).ToListAsync(cancellationToken: cancellationToken), SerializationContext.Default.IEnumerableDetectionExport, cancellationToken: cancellationToken);
                return exportPath;
            });

        var path = new TextPath(exportPath)
            .LeafColor(Color.Green);
        var panel = new Panel(path);
        panel.Header($"[white on darkgreen]{Path.GetFileNameWithoutExtension(exportPath)}[/]").HeavyBorder();
        AnsiConsole.Write(panel);
    }

    private async ValueTask ExportByKeywordAsync(CancellationToken cancellationToken)
    {
        var keyword = await AnsiConsole.PromptAsync(new TextPrompt<string>("Enter a [bold]keyword[/] to filter detections:"), cancellationToken);

        var exportPath = await AnsiConsole.Status()
            .AutoRefresh(true)
            .StartAsync("Exporting...", async _ =>
            {
                var exportPath = Path.Join(exportDirectory, $"Keyword-{DateTime.Now:O}-{keyword}.json".CleanFileName()).CleanFilePath();
                await using var stream = File.OpenWrite(exportPath);
                await JsonSerializer.SerializeAsync(stream, await detectionRepository.EnumerateDetectionsByKeywordAsync(keyword, cancellationToken).ToListAsync(cancellationToken: cancellationToken), SerializationContext.Default.IEnumerableDetectionExport, cancellationToken: cancellationToken);
                return exportPath;
            });

        var path = new TextPath(exportPath)
            .LeafColor(Color.Green);
        var panel = new Panel(path);
        panel.Header($"[white on darkgreen]{Path.GetFileNameWithoutExtension(exportPath)}[/]").HeavyBorder();
        AnsiConsole.Write(panel);
    }
}