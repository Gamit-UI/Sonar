using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Sonar.Detections;
using Sonar.Metrics.Services;
using Sonar.Rules.Builders;
using Sonar.Rules.Extensions;
using Sonar.Rules.Serialization;
using Sonar.Rules.Serialization.Yaml;
using YamlDotNet.Core;

namespace Sonar.Rules.Helpers;

internal static class RuleHelper
{
    private static readonly ISet<string> Default = new HashSet<string>();
    
    public static bool TryDeserialize(ILogger logger, string yamlString, Aliases aliases, Details details, ChannelAbbrevations channelAbbreviations, ProviderAbbrevations providerAbbreviations, [MaybeNullWhen(false)] out RuleBase rule, [MaybeNullWhen(false)] out ISet<string> properties, [MaybeNullWhen(true)] out string error)
    {
        rule = null;
        error = null;
        properties = null;
        var metadata = new RuleMetadata();
        try
        {
            var yamlRules = YamlParser.DeserializeMany<YamlRule>(yamlString).ToList();
            metadata = yamlRules.ToMetadata();
            rule = RuleBuilder.Build(yamlRules, metadata, aliases, details, channelAbbreviations, providerAbbreviations, domainControllers: Default, out _, out properties);
            return true;
        }
        catch (Exception ex)
        {
            if (ex is Parlot.ParseException parseException)
            {
                error = $"[{parseException.Source} {parseException.Position}]: {parseException.Message}";
            }
            else if (ex is YamlException yamlException)
            {
                error = yamlException.ToString();
            }
            else
            {
                error = ex.Message;
            }
            
            logger.LogError(ex, "An error has occurred while deserializing the rule {Rule}: {Message}", metadata.Id, ex.Message);
            return false;
        }
    }
    
    public static bool TryDeserialize(ILogger logger, IList<YamlRule> yamlRules, RuleFilter filter, Aliases aliases, Details details, ChannelAbbrevations channels, ProviderAbbrevations providers, ExcludedRules excluded, NoisyRules noisy, ISet<string> domainControllers, [MaybeNullWhen(false)] out RuleBase rule, [MaybeNullWhen(false)] out ISet<string> properties, [MaybeNullWhen(false)] out ISet<ushort> eventIds, [MaybeNullWhen(true)] out string error)
    {
        rule = null;
        error = null;
        properties = null;
        eventIds = null;
        var metadata = new RuleMetadata();
        try
        {
            metadata = yamlRules.ToMetadata();
            rule = RuleBuilder.Build(yamlRules, metadata, aliases, details, channels, providers, domainControllers, out eventIds, out properties);
            var status = metadata.Status.FromStatus();
            MetricService.AddRule();
            if (eventIds.Count == 0 ||
                noisy.Items.Contains(metadata.Id) ||
                excluded.Items.Contains(metadata.Id) ||
                status is DetectionStatus.Unsupported ||
                status is DetectionStatus.Deprecated ||
                string.IsNullOrWhiteSpace(metadata.Description) ||
                !filter(metadata, eventIds))
            {
                error = $"The rule {rule.Id} has been filtered";
                return false;
            }

            MetricService.AddEnabledRule();
            return true;
        }
        catch (Exception ex)
        {
            if (ex is Parlot.ParseException parseException)
            {
                error = $"[{parseException.Source} {parseException.Position}]: {parseException.Message}";
            }
            else if (ex is YamlException yamlException)
            {
                error = yamlException.ToString();
            }
            else
            {
                error = ex.Message;
            }
            
            logger.LogError(ex, "An error has occurred while deserializing the rule {Rule}: {Message}", metadata.Id, ex.Message);
            return false;
        }
    }
}