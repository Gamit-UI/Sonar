using System.Linq.Expressions;
using Sonar.Detections;
using Sonar.Rules.Builders.Walkers;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Extensions;
using Sonar.Rules.Predicates;
using Sonar.Rules.Serialization;
using Sonar.Rules.Serialization.Yaml;

namespace Sonar.Rules.Builders;

internal static class RuleBuilder
{
    public static RuleBase Build(IList<YamlRule> yamlRules, RuleMetadata ruleMetadata, Aliases aliases, Details details, ChannelAbbrevations channelAbbreviations, ProviderAbbrevations providerAbbreviations, ISet<string> domainControllers, out ISet<ushort> eventIds, out ISet<string> properties)
    {
        var detailsPredicate = BuildDetailsPredicate();
        if (ruleMetadata.CorrelationOrAggregationTimeSpan.HasValue)
        {
            var aggregationPredicate = BuildAggregationPredicate(yamlRules, ruleMetadata, aliases, details, channelAbbreviations, providerAbbreviations, domainControllers, out eventIds, out properties);
            return new AggregationRule(ruleMetadata, aggregationPredicate.Predicate, aggregationPredicate.Aggregate, detailsPredicate.Predicate, aggregationPredicate.AggregationProperties);
        }
        else
        {
            var winEventPredicate = BuildWinEventPredicate(yamlRules, ruleMetadata, aliases, details, channelAbbreviations, providerAbbreviations, domainControllers, out eventIds, out properties);
            return new StandardRule(ruleMetadata, winEventPredicate.Predicate, detailsPredicate.Predicate);
        }
    }

    private static WinEventPredicate BuildWinEventPredicate(IList<YamlRule> yamlRules, RuleMetadata ruleMetadata, Aliases aliases, Details details, ChannelAbbrevations channelAbbreviations, ProviderAbbrevations providerAbbreviations, ISet<string> domainControllers, out ISet<ushort> eventIds, out ISet<string> properties)
    {
        var expression = yamlRules.BuildRuleExpression<Expression<Func<WinEvent, bool>>>(ruleMetadata, buildableExpression => buildableExpression.BuildPredicateExpression(), aliases, details, channelAbbreviations, providerAbbreviations, domainControllers, out eventIds, out properties);
        return new WinEventPredicate(expression.Compile());
    }
    
    private static AggregationPredicate BuildAggregationPredicate(IList<YamlRule> yamlRules, RuleMetadata ruleMetadata, Aliases aliases, Details details, ChannelAbbrevations channelAbbreviations, ProviderAbbrevations providerAbbreviations, ISet<string> domainControllers, out ISet<ushort> eventIds, out ISet<string> properties)
    {
        var expressions = yamlRules.BuildRuleExpression<Tuple<Expression<Func<WinEvent, bool>>, Expression<Func<WinEvent?>>, ISet<string>>>(ruleMetadata, buildableExpression => buildableExpression.BuildAggregationExpression(), aliases, details, channelAbbreviations, providerAbbreviations, domainControllers, out eventIds, out properties);
        return new AggregationPredicate(expressions.Item1.Compile(), expressions.Item2.Compile(), expressions.Item3);
    }

    private static DetailsPredicate BuildDetailsPredicate()
    {
        var expression = YamlRuleExtensions.BuildDetailsExpression();
        return new DetailsPredicate(expression.Compile());
    }
    
    public static IDictionary<string, DetectionExpressions> GetDetectionExpressionsByName(YamlRule yamlRule, ISet<string> domainControllers, Func<string, bool> canProcessRegex, Action<string> onRegexFailure)
    {
        return EnumerateDetections(yamlRule).Select(detection => new KeyValuePair<string, DetectionExpressions>(detection.Name, GetDetectionExpressions(detection, domainControllers, canProcessRegex, onRegexFailure))).ToDictionary(StringComparer.Ordinal);
    }
    
    private static IEnumerable<Detection> EnumerateDetections(YamlRule yamlRule)
    {
        foreach (var detection in yamlRule.Detections)
        {
            if (detection.Key.Equals(Constants.Condition)) continue;
            yield return Detection.Create(detection);
        }
    }

    private static DetectionExpressions GetDetectionExpressions(Detection detection, ISet<string> domainControllers, Func<string, bool> canProcessRegex, Action<string> onRegexFailure)
    {
        var expressions = ExtractExpression(detection.Properties, domainControllers, canProcessRegex, onRegexFailure);
        return new DetectionExpressions(expressions);
    }

    private static Expression<Func<WinEvent, bool>> ExtractExpression(object properties, ISet<string> domainControllers, Func<string, bool> canProcessRegex, Action<string> onRegexFailure)
    {
        if (properties is IDictionary<string, object> dictionary)
        {
            return DictionaryWalker.Walk(dictionary, domainControllers, canProcessRegex, onRegexFailure);
        }

        if (properties is IEnumerable<object> enumerable)
        {
            return EnumerableWalker.Walk(enumerable, domainControllers, canProcessRegex, onRegexFailure);
        }

        return PredicateBuilder.New<WinEvent>(defaultExpression: false);
    }
}