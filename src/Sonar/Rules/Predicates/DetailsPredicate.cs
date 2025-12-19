using Sonar.Detections;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Predicates;

internal sealed record DetailsPredicate(Func<WinEvent, RuleMetadata, DetectionDetails> Predicate);