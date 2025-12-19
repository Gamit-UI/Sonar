using Microsoft.Extensions.Logging.Abstractions;
using Sonar.Rules;
using Sonar.Rules.Aggregations.Stores;
using Sonar.Rules.Helpers;
using Sonar.Rules.Stores;
using Sonar.Setup.Services.NonInteractive;
using Sonar.Setup.Services.Options;

namespace Sonar.Tests.Detection;

public sealed class RuleFixture : IAsyncLifetime
{
    public readonly IDictionary<string, StandardRule> StandardRules = new Dictionary<string, StandardRule>();
    
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        using var client = new HttpClient();
        var setupOptions = new SetupOptions(Interactive: false, Online: false, (in _, in _) => true);
        var setupService = new NonInteractiveSetupService(NullLogger<NonInteractiveSetupService>.Instance, client);
        await ConfigHelper.InitializeAsync(setupService, setupOptions, new Progress<double>(), CancellationToken.None);
        var ruleStore = new RuleStore(NullLogger<RuleStore>.Instance, new PropertyStore(), setupService);
        await ruleStore.InitializeAsync(setupOptions, new Progress<double>(), () => { }, _ => { }, CancellationToken.None);
        foreach (var standardRule in ruleStore.EnumerateStandardRules())
        {
            StandardRules.TryAdd(standardRule.Id, standardRule);
        }
    }
}
