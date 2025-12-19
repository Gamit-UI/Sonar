using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Detections;
using Sonar.Extensions;
using Sonar.Rules;
using Sonar.Rules.Extensions;
using Sonar.Rules.Helpers;
using Sonar.Setup.Services.Options;
using Spectre.Console;

namespace Sonar.Setup.Services.Interactive;

internal sealed class InteractiveSetupService(ILogger<InteractiveSetupService> logger, IHostApplicationLifetime hostApplicationLifetime, HttpClient httpClient) : SetupService(httpClient)
{
    private enum Connectivity
    {
        Offline,
        Online
    }

    private enum Configuration
    {
        Profile,
        Custom
    }

    public override async ValueTask<SetupOptions> InitializeAsync(CancellationToken cancellationToken)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            logger.LogError("Environment does not support interaction. Exiting...");
            hostApplicationLifetime.StopApplication();
            return new SetupOptions(Interactive: false, Online: false, Filter: (in _, in _) => true);
        }

        AnsiConsole.Write(new Rows(new FigletText("Sonar").Centered().Color(Color.Blue), new Text("A real-time Sigma rule scanner", new Style(foreground: Color.White, decoration: Decoration.Italic)).Centered()));
        AnsiConsole.Write(new Rule("[blue]Information[/]").LeftJustified());
        AnsiConsole.MarkupLine(AnsiConsole.Profile.Capabilities.Links ? "Sonar scans Windows Event Logs in real-time against [link=https://github.com/Yamato-Security/hayabusa-rules]high-quality[/] Sigma ruleset to identify indicators of compromise (IoCs) and anomalies." : "Sonar scans Windows Event Logs in real-time against high-quality Sigma ruleset (https://github.com/Yamato-Security/hayabusa-rules) to identify indicators of compromise (IoCs) and anomalies.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Version [bold]{typeof(SetupService).Assembly.GetVersion()}[/]");

        if (!IsAdministrator())
        {
            AnsiConsole.Write(new Rule("[yellow]Warning[/]").LeftJustified());
            AnsiConsole.MarkupLine("[yellow]Sonar is not running as Administrator. Security channel may not be consumed.[/]");
            logger.LogWarning("Sonar is not running as Administrator. Security channel may not be consumed.");
        }

        AnsiConsole.Write(new Rule("[green]Configuration[/]").LeftJustified());
        var connectivity = await GetConnectivityAsync(cancellationToken);
        var filter = await GetFilterAsync(cancellationToken);
        return new SetupOptions(Interactive: true, Online: connectivity == Connectivity.Online, filter);
    }

    private static async ValueTask<Connectivity> GetConnectivityAsync(CancellationToken cancellationToken)
    {
        return await AnsiConsole.PromptAsync(new SelectionPrompt<Connectivity>()
            .UseConverter(value =>
            {
                return value switch
                {
                    Connectivity.Offline => "No",
                    Connectivity.Online => "Yes",
                    _ => throw new ArgumentOutOfRangeException()
                };
            })
            .WrapAround()
            .Title("Allow Sonar to download up-to-date Sigma rules")
            .AddChoices(Connectivity.Offline, Connectivity.Online), cancellationToken);
    }

    private static async ValueTask<RuleFilter> GetFilterAsync(CancellationToken cancellationToken)
    {
        var configuration = await AnsiConsole.PromptAsync(new SelectionPrompt<Configuration>()
            .UseConverter(value =>
            {
                return value switch
                {
                    Configuration.Profile => "[bold]Profile[/] - Enable rules based on a profile ([italic]Core[/], [italic]Core+[/], [italic]Core++[/])",
                    Configuration.Custom => "[bold]Custom[/] - Enable rules based on [italic]severity[/] and [italic]status[/]",
                    _ => throw new ArgumentOutOfRangeException()
                };
            })
            .WrapAround()
            .Title("Select Sigma rules to enable")
            .AddChoices(Configuration.Profile, Configuration.Custom), cancellationToken);

        if (configuration == Configuration.Profile)
        {
            var profile = await AnsiConsole.PromptAsync(new SelectionPrompt<DetectionProfile>()
                .Title("Select a [bold]profile[/]:")
                .WrapAround()
                .UseConverter(value =>
                {
                    return value switch
                    {
                        DetectionProfile.Core => "[bold]Core[/] - High-quality rules that are unlikely to generate false positives",
                        DetectionProfile.CorePlus => "[bold]Core+[/] - Rules that may inadvertently match legitimate applications",
                        DetectionProfile.CorePlusPlus => "[bold]Core++[/] - Rules that aim to detect threats as early as possible leading to a higher volume of false positives",
                        _ => throw new ArgumentOutOfRangeException()
                    };
                })
                .AddChoices(DetectionProfile.Core, DetectionProfile.CorePlus, DetectionProfile.CorePlusPlus), cancellationToken);

            return (in metadata, in eventIds) =>
            {
                if (eventIds.Count == 0) return false;
                var severity = metadata.Level.FromLevel();
                var status = metadata.Status.FromStatus();
                return ProfileHelper.ShouldBeEnabled(metadata.Id, profile, severity, status, eventIds.Select(eventId => AuditPolicyMapping.VolumeByEventId.TryGetValue(eventId, out var volume) ? volume : AuditPolicyVolume.Low).Aggregate((left, right) => left > right ? left : right));
            };
        }

        if (configuration == Configuration.Custom)
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

            var statuses = await AnsiConsole.PromptAsync(new MultiSelectionPrompt<DetectionStatus>()
                .Title("Select [bold]statuses[/]:")
                .PageSize(5)
                .Required()
                .WrapAround()
                .MoreChoicesText("[grey](Move up and down to reveal more statuses)[/]")
                .UseConverter(value => Enum.GetName(value)!)
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a status, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(DetectionStatus.Unsupported, DetectionStatus.Deprecated, DetectionStatus.Experimental, DetectionStatus.Test, DetectionStatus.Stable), cancellationToken);

            return (in metadata, in eventIds) =>
            {
                if (eventIds.Count == 0) return false;
                var severity = metadata.Level.FromLevel();
                var status = metadata.Status.FromStatus();
                return severities.Any(value => value.HasFlag(severity)) && statuses.Any(value => value.HasFlag(status));
            };
        }

        return (in _, in eventIds) =>
        {
            if (eventIds.Count == 0) return false;
            return true;
        };
    }
}