using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Sonar.Rules.Expressions.Predicates;

internal sealed class ExpressionStarter<T>
{
    private Expression<Func<T, bool>>? predicate;

    internal ExpressionStarter()
        : this(false)
    {
    }

    internal ExpressionStarter(bool defaultExpression)
    {
        if (defaultExpression)
            DefaultExpression = (Expression<Func<T, bool>>)(f => true);
        else
            DefaultExpression = (Expression<Func<T, bool>>)(f => false);
    }

    internal ExpressionStarter(Expression<Func<T, bool>> exp)
        : this(false)
    {
        predicate = exp;
    }

    private Expression<Func<T, bool>> Predicate => !IsStarted && UseDefaultExpression ? DefaultExpression! : predicate!;

    public bool IsStarted => predicate != null;

    public bool UseDefaultExpression => DefaultExpression != null;

    public Expression<Func<T, bool>>? DefaultExpression { get; }

    public Expression<Func<T, bool>> Start(Expression<Func<T, bool>> exp)
    {
        if (IsStarted)
            throw new Exception("Predicate cannot be started again.");
        return predicate = exp;
    }

    public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> expr2)
    {
        return !IsStarted ? Start(expr2) : predicate = Predicate.Or(expr2);
    }

    public Expression<Func<T, bool>> And(Expression<Func<T, bool>> expr2)
    {
        return !IsStarted ? Start(expr2) : predicate = Predicate.And(expr2);
    }

    public Expression<Func<T, bool>>? Not()
    {
        if (IsStarted)
            predicate = Predicate.Not();
        else
            Start(x => false);
        return predicate;
    }

    public override string ToString()
    {
        return Predicate.ToString();
    }

    public static implicit operator Expression<Func<T, bool>>(ExpressionStarter<T> right)
    {
        return right.Predicate;
    }

    public static implicit operator Func<T, bool>(ExpressionStarter<T> right)
    {
        return right.Predicate.Compile();
    }

    public static implicit operator ExpressionStarter<T>(Expression<Func<T, bool>> right)
    {
        return new ExpressionStarter<T>(right);
    }

    public Func<T, bool> Compile() => Predicate.Compile();

    public Expression Body => Predicate.Body;

    public ExpressionType NodeType => Predicate.NodeType;

    public ReadOnlyCollection<ParameterExpression> Parameters => Predicate.Parameters;

    public Type Type => Predicate.Type;

    public string? Name => Predicate.Name;

    public Type ReturnType => Predicate.ReturnType;

    public bool TailCall => Predicate.TailCall;

    public bool CanReduce => Predicate.CanReduce;
}