using System.Linq.Expressions;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Expressions.Operators;

internal sealed class AndBuildableExpression(BuildableExpression left, BuildableExpression right) : BinaryBuildableExpression(left, right)
{
    public override Expression<Func<WinEvent, bool>> BuildPredicateExpression()
    {
        return Left.BuildPredicateExpression().And(Right.BuildPredicateExpression());
    }
}