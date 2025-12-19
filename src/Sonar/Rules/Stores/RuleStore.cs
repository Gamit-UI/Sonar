using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ConcurrentCollections;
using Microsoft.Extensions.Logging;
using Sonar.Helpers;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Extensions;
using Sonar.Rules.Helpers;
using Sonar.Rules.Serialization.Yaml;
using Sonar.Setup.Services;
using Sonar.Setup.Services.Options;

namespace Sonar.Rules.Stores;

internal sealed class RuleStore(ILogger<RuleStore> logger, IPropertyStore propertyStore, ISetupService setupService) : IRuleStore
{
    private readonly ConcurrentDictionary<string, string> linkByRuleTitle = new();
    private readonly ConcurrentDictionary<string, KeyValuePair<string, string>> tacticByRuleTitle = new();
    private readonly ConcurrentDictionary<ushort, ConcurrentHashSet<StandardRule>> standardRules = new();
    private readonly ConcurrentDictionary<ushort, ConcurrentHashSet<AggregationRule>> aggregationRules = new();

    public async ValueTask<int> InitializeAsync(SetupOptions setupOptions, IProgress<double> progress, Action onBeforeCount, Action<int> onCount, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var source = await setupService.GetEncodedRulesAsync(setupOptions, progress, cancellationToken);
        using var reader = new StreamReader(source);
        onBeforeCount();
        
        var groups = ParseRules(reader).GroupBy(rule => rule.Rulefile).ToList();
        var ruleCount = groups.Sum(group => group.Count());
        onCount(ruleCount);
        
        var aliases = ConfigHelper.GetAliases();
        var details = ConfigHelper.GetDetails();
        var channels = ConfigHelper.GetChannelAbbreviations();
        var providers = ConfigHelper.GetProviderAbbreviations();
        var excluded = ConfigHelper.GetExcludedRules();
        var noisy = ConfigHelper.GetNoisyRules();
        var domainControllers = GetDomainControllers();
        Parallel.ForEach(groups, group =>
        {
            if (!RuleHelper.TryDeserialize(logger, group.ToList(), setupOptions.Filter, aliases, details, channels, providers, excluded, noisy, domainControllers, out var rule, out var properties, out var eventIds, out var error))
            {
                logger.LogWarning(error);
                return;
            }

            switch (rule)
            {
                case StandardRule standardRule:
                    Add(eventIds, standardRule, standardRules);
                    break;
                case AggregationRule aggregationRule:
                    propertyStore.AddProperties(aggregationRule.Id, properties);
                    Add(eventIds, aggregationRule, aggregationRules);
                    break;
            }

            Interlocked.Increment(ref count);
            progress.Report((double)count / ruleCount);
        });

        logger.LogInformation("{Count} rules loaded", count);
        return count;
    }

    public bool TryGetStandardRules(ushort id, [MaybeNullWhen(false)] out ConcurrentHashSet<StandardRule> rules)
    {
        return standardRules.TryGetValue(id, out rules);
    }

    public bool TryGetAggregationRules(ushort id, [MaybeNullWhen(false)] out ConcurrentHashSet<AggregationRule> rules)
    {
        return aggregationRules.TryGetValue(id, out rules);
    }

    public IEnumerable<StandardRule> EnumerateStandardRules()
    {
        return standardRules.Values.SelectMany(value => value);
    }

    public bool TryGetLink(string ruleTitle, [MaybeNullWhen(false)] out string link)
    {
        return linkByRuleTitle.TryGetValue(ruleTitle, out link) && !string.IsNullOrWhiteSpace(link);
    }

    public bool TryGetTactic(string ruleTitle, [MaybeNullWhen(false)] out string tactic, [MaybeNullWhen(false)] out string link)
    {
        tactic = null;
        link = null;
        if (tacticByRuleTitle.TryGetValue(ruleTitle, out var mitre))
        {
            tactic = mitre.Key;
            link = mitre.Value;
            return true;
        }

        return false;
    }

    public IEnumerable<string> GetRuleTitles()
    {
        return standardRules.SelectMany(kvp => kvp.Value.Select(rule => rule.Metadata.Title)).Concat(aggregationRules.SelectMany(kvp => kvp.Value.Select(rule => rule.Metadata.Title)));
    }

    private void Add<T>(ISet<ushort> eventIds, T rule, ConcurrentDictionary<ushort, ConcurrentHashSet<T>> store) where T : RuleBase
    {
        linkByRuleTitle.TryAdd(rule.Metadata.Title, rule.Metadata.Link);
        if (rule.TryGetMitreTactic(out var tactic, out var link))
        {
            tacticByRuleTitle.TryAdd(rule.Metadata.Title, new KeyValuePair<string, string>(tactic, link));
        }
        
        foreach (var eventId in eventIds)
        {
            store.AddOrUpdate(eventId, addValueFactory: _ => [rule], updateValueFactory: (_, current) =>
            {
                current.Add(rule);
                return current;
            });
        }
    }

    private ISet<string> GetDomainControllers()
    {
        try
        {
            return DomainHelper.EnumerateDomainControllers().ToHashSet();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
            return new HashSet<string>();
        }
    }

    private static IEnumerable<YamlRule> ParseRules(StreamReader reader)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var line = reader.ReadLine();
            if (line is null) yield break;
            
            sb.AppendLine(line);
            if (line.Contains("rulefile:", StringComparison.Ordinal))
            {
                foreach (var rule in YamlParser.DeserializeMany<YamlRule>(sb.ToString()))
                {
                    if (!rule.Rulefile.Contains("/builtin/", StringComparison.Ordinal)) continue;
                    if (rule.Tags.Contains("sysmon", StringComparer.OrdinalIgnoreCase)) continue;
                    yield return rule;
                }

                sb.Clear();
            }
        }
    }
}