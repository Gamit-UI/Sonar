using System.Linq.Expressions;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Expressions.Operators;

internal sealed class NegateBuildableExpression(BuildableExpression inner) : UnaryBuildableExpression(inner)
{
    public override Expression<Func<WinEvent, bool>> BuildPredicateExpression()
    {
        return Inner.BuildPredicateExpression().Not();
    }
}