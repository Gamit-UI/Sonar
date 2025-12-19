namespace Sonar.Detections;

[Flags]
internal enum DetectionStatus
{
    Unsupported = 1,
    Deprecated = 2,
    Experimental = 4,
    Test = 8,
    Stable = 16
}