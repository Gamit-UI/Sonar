using System.Text.Json.Serialization;

namespace Sonar.Detections;

public sealed record DetectionExport([property:JsonPropertyName("Date")]string Date, [property:JsonPropertyName("Computer")] string Computer, [property:JsonPropertyName("Level")]string Level, [property:JsonPropertyName("Rule")]string Rule, [property:JsonPropertyName("Link")]string Link, [property:JsonPropertyName("Details")]IDictionary<string, string> Details);