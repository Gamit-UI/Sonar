using System.Collections;
using System.Linq.Expressions;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Builders.Walkers;

internal static class DictionaryWalker
{
    public static Expression<Func<WinEvent, bool>> Walk(IDictionary<string, object> properties, ISet<string> domainControllers, Func<string, bool> canProcessRegex, Action<string> onRegexFailure, string? parentNodeName = null)
    {
        var expression = PredicateBuilder.New<WinEvent>(defaultExpression: true);
        foreach (var property in properties)
        {
            if (property.Value is string value)
            {
                var currentExpression = ExpressionBuilder.BuildMatchExpression(property.Key, value, parentNodeName, domainControllers, canProcessRegex, onRegexFailure);
                expression = expression.And(currentExpression);
            }
            else if (property.Value is IEnumerable<object> enumerable)
            {
                var currentExpression = EnumerableWalker.Walk(properties: enumerable.Select(prop => new KeyValuePair<string, object>(property.Key, prop)).Cast<object>(), domainControllers, canProcessRegex, onRegexFailure, parentNodeName: property.Key, shouldBeAnd: property.Key.Contains(Constants.All));
                expression = expression.And(currentExpression);
            }
            else if (property.Value is IDictionary dictionary)
            {
                switch (dictionary)
                {
                    case IDictionary<string, object> objectValue:
                        var currentExpression = Walk(objectValue, domainControllers, canProcessRegex, onRegexFailure, parentNodeName: property.Key);
                        expression = expression.And(currentExpression);
                        break;
                    case IDictionary<string, string> stringValue:
                        currentExpression = Walk(properties: stringValue.ToDictionary(kvp => kvp.Key, object (kvp) => kvp.Value, StringComparer.Ordinal), domainControllers, canProcessRegex, onRegexFailure, parentNodeName: property.Key);
                        expression = expression.And(currentExpression);
                        break;
                    default:
                        throw new Exception($"Value of type {dictionary.GetType()} is not supported");
                }
            }
            else
            {
                throw new Exception($"Property of type {property.Value.GetType()} is not supported");
            }
        }

        return expression;
    }
}