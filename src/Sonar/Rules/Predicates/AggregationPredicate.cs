using Sonar.Rules.Serialization;

namespace Sonar.Rules.Predicates;

internal sealed record AggregationPredicate(Func<WinEvent, bool> Predicate, Func<WinEvent?> Aggregate, ISet<string> AggregationProperties);