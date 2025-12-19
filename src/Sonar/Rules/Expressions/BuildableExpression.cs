using System.Linq.Expressions;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Expressions;

internal class BuildableExpression
{
    public virtual Expression<Func<WinEvent, bool>> BuildPredicateExpression()
    {
        throw new NotImplementedException();
    }

    public virtual Tuple<Expression<Func<WinEvent, bool>>, Expression<Func<WinEvent?>>, ISet<string>> BuildAggregationExpression()
    {
        throw new NotImplementedException();
    }
}