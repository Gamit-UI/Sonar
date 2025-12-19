using DEXS.Console.FancyProgress;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Databases.Repositories;
using Sonar.Events.Processors;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Stores;
using Sonar.Setup.Services;
using Sonar.Setup.Services.Options;
using Spectre.Console;

namespace Sonar.Setup.Hosting.Interactive;

internal sealed class InteractiveSetupHostedService(ILogger<InteractiveSetupHostedService> logger, IHostApplicationLifetime hostApplicationLifetime, ISetupService setupService, IDetectionRepository detectionRepository, IRuleStore ruleStore, IAggregationRepository aggregationRepository, IPropertyStore propertyStore, IEventProcessor eventProcessor) : SetupHostedService(setupService, detectionRepository, ruleStore, aggregationRepository, propertyStore, eventProcessor)
{
    private async ValueTask LoadRuleConfigurationAsync(SetupOptions setupOptions, ProgressContext context, CancellationToken cancellationToken)
    {
        var task = context.AddTask("Loading configuration");
        task.StartTask();
        task.MaxValue = 1.0d;
        await base.LoadRuleConfigurationAsync(setupOptions, task, cancellationToken);
        task.StopTask();
    }

    private async ValueTask LoadRulesAsync(SetupOptions setupOptions, ProgressContext context, CancellationToken cancellationToken)
    {
        var task = context.AddTask("Loading rules");
        task.StartTask();
        task.MaxValue = 1.0d;
        await base.LoadRulesAsync(setupOptions, task, onBeforeCount: () =>
        {
            task.IsIndeterminate = true;
            task.Description = "Extracting rules";
            task.Value = 0d;
        }, onCount: count =>
        {
            task.IsIndeterminate = false;
            task.Description = $"Initializing {count} rules";
        }, cancellationToken);
        task.StopTask();
    }
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var setupOptions = await InitializeSetupAsync(cancellationToken);
            AnsiConsole.Write(new Rule("[white]Initialization[/]").LeftJustified());
            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(true)
                .HideCompleted(false)
                .Columns(new TaskDescriptionColumn(),
                    new FancyProgressBarColumn
                    {
                        Width = 40,
                        CompletedStyle = new Style(foreground: Color.Lime),
                        CompletedTailStyle = new Style(foreground: Color.Green),
                        RemainingStyle = new Style(foreground: Color.Grey35),
                        ProgressPattern = ProgressPattern.Known.LowerEighthBlocks
                    },
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn
                    {
                        Spinner = Spinner.Known.Dots12
                    })
                .StartAsync(async ctx =>
                {
                    InitializeEventProcessor();
                    InitializeAggregator();
                    await ExtractDependenciesAsync(setupOptions, cancellationToken);
                    await InitializeDatabaseAsync(cancellationToken);
                    await LoadRuleConfigurationAsync(setupOptions, ctx, cancellationToken);
                    await LoadRulesAsync(setupOptions, ctx, cancellationToken);
                });
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
            hostApplicationLifetime.StopApplication();
        }
    }
}