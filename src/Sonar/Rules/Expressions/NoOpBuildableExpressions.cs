using System.Linq.Expressions;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Expressions;

internal sealed class NoOpBuildableExpressions : BuildableExpression
{
    private Expression<Func<WinEvent, bool>> Value { get; } = PredicateBuilder.New<WinEvent>(defaultExpression: false);

    public override Expression<Func<WinEvent, bool>> BuildPredicateExpression()
    {
        return Value;
    }
}