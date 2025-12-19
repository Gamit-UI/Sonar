using System.Linq.Expressions;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Expressions;

internal sealed class ReducedBuildableExpression(Expression<Func<WinEvent, bool>> value) : BuildableExpression
{
    private Expression<Func<WinEvent, bool>> Value { get; } = value;

    public override Expression<Func<WinEvent, bool>> BuildPredicateExpression()
    {
        return Value;
    }
}