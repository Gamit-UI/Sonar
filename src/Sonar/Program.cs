using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Sonar;
using Sonar.Databases.Contexts;
using Sonar.Databases.Repositories;
using Sonar.Detections.Services;
using Sonar.Detections.Services.Interactive;
using Sonar.Detections.Services.NonInteractive;
using Sonar.Events.Pipelines;
using Sonar.Events.Processors;
using Sonar.Events.Services;
using Sonar.Extensions;
using Sonar.Helpers;
using Sonar.Logging;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Aggregations.Stores;
using Sonar.Rules.Serialization;
using Sonar.Rules.Stores;
using Sonar.Setup.Hosting.Interactive;
using Sonar.Setup.Hosting.NonInteractive;
using Sonar.Setup.Services;
using Sonar.Setup.Services.Interactive;
using Sonar.Setup.Services.NonInteractive;

return await MutexHelper.ExecuteOnceAsync("dfeebf82-2815-4eca-80c8-b00f68feca51", async () =>
{
    try
    {
        // TODO: change it to "interactive" option, false by default when frontend is implemented
        var noInteractiveOption = new Option<bool>("--no-interactive", "-n")
        {
            Description = "When set, Sonar run headless. Default to false.",
            DefaultValueFactory = _ => false
        };

        RootCommand root = new() { noInteractiveOption };
        root.SetAction(async (result, ct) =>
        {
            var builder = Host.CreateDefaultBuilder(args);
            using var host = builder
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureHostConfiguration(configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); })
                .ConfigureAppConfiguration((_, configurationBuilder) => { configurationBuilder.AddCommandLine(args); })
                .ConfigureLogging(loggingBuilder => { loggingBuilder.ClearProviders(); })
                .UseSerilog((_, sp, configuration) =>
                {
                    configuration
                        .Enrich.WithAssemblyVersion()
                        .Enrich.WithSourceContext()
                        .Enrich.FromLogContext()
                        .WriteTo.Sink(new SinkInterceptor(sp.CreateScope()))
                        .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "Sonar.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2, fileSizeLimitBytes: 100 * 1024 * 1024, rollOnFileSizeLimit: true, outputTemplate: "[{Timestamp:u} {Level:u}] {Message:lj} {Properties}{NewLine}{Exception}", shared: true);
                }, preserveStaticLogger: false, writeToProviders: false)
                .ConfigureServices(services =>
                {
                    services.AddHttpClient<SetupService>().AddPolicyHandler(GetRetryPolicy());

                    services.AddSingleton<DetectionContext>();
                    services.AddSingleton<IDetectionRepository, DetectionRepository>();
                    services.AddSingleton<IAggregationRepository, AggregationRepository>();

                    services.AddSingleton<IRuleStore, RuleStore>();
                    services.AddSingleton<IPropertyStore, PropertyStore>();

                    services.AddSingleton<IEventProcessor, EventProcessor>();
                    services.AddSingleton<IEventLogPipeline<WinEvent>, WinEventLogPipeline>();
                    services.AddSingleton<IEventService, EventService>();

                    if (Environment.UserInteractive && !result.GetValue(noInteractiveOption))
                    {
                        services.AddSingleton<IDetectionService, InteractiveDetectionService>();
                        services.AddSingleton<ISetupService, InteractiveSetupService>();
                        services.AddHostedService<InteractiveSetupHostedService>();
                    }
                    else
                    {
                        services.AddSingleton<IDetectionService, NonInteractiveDetectionService>();
                        services.AddSingleton<ISetupService, NonInteractiveSetupService>();
                        services.AddHostedService<NonInteractiveSetupHostedService>();
                    }

                    services.AddHostedService<SonarHostedService>();
                })
                .UseWindowsService()
                .Build();

            await host.RunAsync(ct);
        });

        return await root.Parse(args).InvokeAsync();
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex, "Host terminated unexpectedly: {Message}", ex.Message);
        return 1;
    }
    finally
    {
        await Log.CloseAndFlushAsync();
    }
});

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}