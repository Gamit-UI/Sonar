using System.Collections;
using System.Linq.Expressions;
using Sonar.Rules.Expressions.Predicates;
using Sonar.Rules.Serialization;

namespace Sonar.Rules.Builders.Walkers;

internal static class EnumerableWalker
{
    public static Expression<Func<WinEvent, bool>> Walk(IEnumerable<object> properties, ISet<string> domainControllers, Func<string, bool> canProcessRegex, Action<string> onRegexFailure, string? parentNodeName = null, bool shouldBeAnd = false)
    {
        var expression = PredicateBuilder.New<WinEvent>(defaultExpression: true);
        foreach (var property in properties)
        {
            if (property is IEnumerable<object> enumerable)
            {
                var currentExpression = Walk(enumerable, domainControllers, canProcessRegex, onRegexFailure, parentNodeName);
                expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
            }
            else if (property is IDictionary dictionary)
            {
                switch (dictionary)
                {
                    case IDictionary<string, object> objectValue:
                        var currentExpression = DictionaryWalker.Walk(objectValue, domainControllers, canProcessRegex, onRegexFailure, parentNodeName);
                        expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                        break;
                    case IDictionary<string, string> stringValue:
                        currentExpression = DictionaryWalker.Walk(properties: stringValue.ToDictionary(kvp => kvp.Key, object (kvp) => kvp.Value, StringComparer.Ordinal), domainControllers, canProcessRegex, onRegexFailure, parentNodeName);
                        expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                        break;
                    default:
                        throw new Exception($"Value of type {dictionary.GetType()} is not supported");
                }
            }
            else if (property is KeyValuePair<string, object> pair)
            {
                if (pair.Value is string value)
                {
                    Expression<Func<WinEvent, bool>> currentExpression;
                    var multipleValuesHandled = false;
                    if (properties.All(prop => prop is KeyValuePair<string, object> { Value: string }))
                    {
                        currentExpression = ExpressionBuilder.BuildMatchExpression(pair.Key, properties.Select(prop => (string)((KeyValuePair<string, object>)prop).Value).ToList(), parentNodeName, domainControllers, canProcessRegex, onRegexFailure);
                        multipleValuesHandled = true;
                    }
                    else
                    {
                        currentExpression = ExpressionBuilder.BuildMatchExpression(pair.Key, value, parentNodeName, domainControllers, canProcessRegex, onRegexFailure);
                    }
                    
                    expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                    if (multipleValuesHandled) break;
                }
                else if (pair.Value is IEnumerable<object> enumerableValue)
                {
                    var currentExpression = Walk(properties: enumerableValue.Select(prop => new KeyValuePair<string, object>(pair.Key, prop)).Cast<object>(), domainControllers, canProcessRegex, onRegexFailure, parentNodeName);
                    expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                }
                else if (pair.Value is IDictionary innerDictionary)
                {
                    switch (innerDictionary)
                    {
                        case IDictionary<string, object> objectValue:
                            var currentExpression = DictionaryWalker.Walk(objectValue, domainControllers, canProcessRegex, onRegexFailure, parentNodeName: pair.Key);
                            expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                            break;
                        case IDictionary<string, string> stringValue:
                            currentExpression = DictionaryWalker.Walk(properties: stringValue.ToDictionary(kvp => kvp.Key, object (kvp) => kvp.Value, StringComparer.Ordinal), domainControllers, canProcessRegex, onRegexFailure, parentNodeName: pair.Key);
                            expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
                            break;
                        default:
                            throw new Exception($"Value of type {innerDictionary.GetType()} is not supported");
                    }
                }
                else
                {
                    throw new Exception($"Value of type {pair.Value.GetType()} is not supported");
                }
            }
            else if (property is string value)
            {
                var currentExpression = ExpressionBuilder.BuildGrepExpression(value);
                expression = shouldBeAnd ? expression.And(currentExpression) : expression.Or(currentExpression);
            }
            else
            {
                throw new Exception($"Property of type {property.GetType()} is not supported");
            }
        }

        return expression;
    }
}