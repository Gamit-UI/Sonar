namespace Sonar.Detections;

[Flags]
internal enum DetectionSeverity
{
    Informational = 1,
    Low = 2,
    Medium = 4,
    High = 8,
    Critical = 16
}