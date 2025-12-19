using Microsoft.Extensions.Logging;
using Sonar.Detections;
using Sonar.Extensions;
using Sonar.Rules.Serialization;

namespace Sonar.Rules;

internal sealed class AggregationRule(RuleMetadata metadata, Func<WinEvent, bool> rulePredicate, Func<WinEvent?> aggregate, Func<WinEvent, RuleMetadata, DetectionDetails> detailsPredicate, ISet<string> aggregationProperties)
    : RuleBase(metadata)
{
    public bool TryMatch(ILogger logger, out RuleMatch ruleMatch)
    {
        ruleMatch = new RuleMatch();
        try
        {
            var winEvent = aggregate();
            if (winEvent == null) return false;
            ruleMatch = new RuleMatch(true, detailsPredicate(winEvent, Metadata), winEvent);
            return true;
        }
        catch (Exception ex)
        {
            logger.Throttle(nameof(AggregationRule), itself => itself.LogError(ex, "An error has occurred"), expiration: TimeSpan.FromMinutes(1));
            return false;
        }
    }

    public bool TryMatch(ILogger logger, WinEvent winEvent)
    {
        try
        {
            return rulePredicate(winEvent);
        }
        catch (Exception ex)
        {
            logger.Throttle(nameof(AggregationRule), itself => itself.LogError(ex, "An error has occurred"), expiration: TimeSpan.FromMinutes(1));
            return false;
        }
    }

    public ISet<string> AggregationProperties => aggregationProperties;
    public TimeSpan CorrelationOrAggregationTimeSpan => Metadata.CorrelationOrAggregationTimeSpan ?? Constants.DefaultTimeFrame;
}