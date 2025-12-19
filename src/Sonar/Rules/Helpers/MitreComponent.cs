using System.Text.Json.Serialization;

namespace Sonar.Rules.Helpers;

internal sealed record MitreComponent([property:JsonPropertyName("Id")]string Id, [property:JsonPropertyName("Tactic")]string Tactic, [property:JsonPropertyName("Technique")] string Technique, [property:JsonPropertyName("Sub-Technique")]string SubTechnique);