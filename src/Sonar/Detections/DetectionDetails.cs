using Sonar.Rules.Serialization;

namespace Sonar.Detections;

public readonly struct DetectionDetails(string eventTitle, string details, RuleMetadata ruleMetadata, DateTimeOffset timeStamp)
{
    public string EventTitle { get; } = eventTitle;
    public string Details { get; } = details;
    public RuleMetadata RuleMetadata { get; } = ruleMetadata;
    public DateTimeOffset TimeStamp { get; } = timeStamp;
}