using Sonar.Detections;
using Sonar.Rules.Serialization;

namespace Sonar.Rules;

public readonly struct RuleMatch(bool match, DetectionDetails detectionDetails, WinEvent winEvent)
{
    public bool Match { get; } = match;
    public DetectionDetails DetectionDetails { get; } = detectionDetails;
    public DateTimeOffset Date { get; } = winEvent.SystemTime.ToUniversalTime();
    public WinEvent WinEvent { get; } = winEvent;
}