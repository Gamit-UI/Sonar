using Sonar.Rules.Serialization;

namespace Sonar.Rules.Predicates;

internal sealed record WinEventPredicate(Func<WinEvent, bool> Predicate);