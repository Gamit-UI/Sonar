using System.Collections;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Sonar.Rules.Serialization.Yaml.Deserializers;

internal sealed class ListsAcceptScalarDeserializer : INodeDeserializer
{
    private readonly IObjectFactory objectFactory = new DefaultObjectFactory();

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer objectDeserializer)
    {
        value = null;

        if (IsList(expectedType) && parser.Accept<NodeEvent>(out var evt))
        {
            if (TryGetScalar(evt, out var scalar))
            {
                parser.SkipThisAndNestedEvents();
                value = objectFactory.Create(expectedType);
                if (value is IList list)
                {
                    list.Add(scalar);
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryGetScalar(NodeEvent nodeEvent, [MaybeNullWhen(false)] out string scalarValue)
    {
        scalarValue = null;
        if (nodeEvent is Scalar { Style: ScalarStyle.Plain or ScalarStyle.SingleQuoted } scalar)
        {
            scalarValue = scalar.Value;
            return !string.IsNullOrWhiteSpace(scalarValue);
        }

        return false;
    }

    private static bool IsList(Type type)
    {
        return typeof(IList).IsAssignableFrom(type)
               || (type is { IsInterface: true, IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(IList<>));
    }
}