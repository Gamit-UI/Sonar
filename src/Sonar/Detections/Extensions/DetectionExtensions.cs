using Spectre.Console;

namespace Sonar.Detections.Extensions;

internal static class DetectionExtensions
{
    public static Color GetColor(this DetectionSeverity severity)
    {
        return severity switch
        {
            DetectionSeverity.Informational => Color.LightSkyBlue1,
            DetectionSeverity.Low => Color.Yellow,
            DetectionSeverity.Medium => Color.Orange1,
            DetectionSeverity.High => Color.Red,
            DetectionSeverity.Critical => Color.Red3,
            _ => Color.White
        };
    }
}