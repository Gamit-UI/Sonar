using Microsoft.Extensions.Logging;
using Sonar.Detections;
using Sonar.Extensions;
using Sonar.Rules.Serialization;

namespace Sonar.Rules;

public sealed class StandardRule(RuleMetadata metadata, Func<WinEvent, bool> rulePredicate, Func<WinEvent, RuleMetadata, DetectionDetails> detailsPredicate)
    : RuleBase(metadata)
{
    public bool TryMatch(ILogger logger, WinEvent winEvent, out RuleMatch ruleMatch)
    {
        ruleMatch = new RuleMatch();
        try
        {
            var match = rulePredicate(winEvent);
            if (!match) return false;
            ruleMatch = new RuleMatch(match, detailsPredicate(winEvent, Metadata), winEvent);
            return true;
        }
        catch (Exception ex)
        {            
            logger.Throttle(nameof(StandardRule), itself => itself.LogError(ex, "An error has occurred"), expiration: TimeSpan.FromMinutes(1));
            return false;
        }
    }
}