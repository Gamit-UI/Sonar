using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Databases.Repositories;
using Sonar.Events.Processors;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Stores;
using Sonar.Setup.Services;

namespace Sonar.Setup.Hosting.NonInteractive;

internal sealed class NonInteractiveSetupHostedService(ILogger<NonInteractiveSetupHostedService> logger, IHostApplicationLifetime hostApplicationLifetime, ISetupService setupService, IDetectionRepository detectionRepository, IRuleStore ruleStore, IAggregationRepository aggregationRepository, IPropertyStore propertyStore, IEventProcessor eventProcessor) : SetupHostedService(setupService, detectionRepository, ruleStore, aggregationRepository, propertyStore, eventProcessor)
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var setupOptions = await InitializeSetupAsync(cancellationToken);
            InitializeEventProcessor();
            InitializeAggregator();
            await InitializeDatabaseAsync(cancellationToken);
            await LoadRuleConfigurationAsync(setupOptions, new Progress<double>(), cancellationToken);
            await LoadRulesAsync(setupOptions, new Progress<double>(), onBeforeCount: () => { }, onCount: _ => { }, cancellationToken);
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