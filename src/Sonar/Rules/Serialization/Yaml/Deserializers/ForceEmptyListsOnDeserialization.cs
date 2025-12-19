using System.Collections;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Sonar.Rules.Serialization.Yaml.Deserializers;

internal sealed class ForceEmptyListsOnDeserialization : INodeDeserializer
{
    private readonly IObjectFactory objectFactory = new DefaultObjectFactory();

    public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer objectDeserializer)
    {
        value = null;

        if (IsList(expectedType) && parser.Accept<NodeEvent>(out var evt))
        {
            if (NodeIsNull(evt))
            {
                parser.SkipThisAndNestedEvents();
                value = objectFactory.Create(expectedType);
                return true;
            }
        }

        return false;
    }

    private bool NodeIsNull(NodeEvent nodeEvent)
    {
        // http://yaml.org/type/null.html

        if (nodeEvent.Tag == "tag:yaml.org,2002:null")
        {
            return true;
        }

        if (nodeEvent is Scalar { Style: ScalarStyle.Plain or ScalarStyle.SingleQuoted } scalar)
        {
            var value = scalar.Value;
            return value is "" or "~" or "null" or "Null" or "NULL";
        }

        return false;
    }

    private bool IsList(Type type)
    {
        return typeof(IList).IsAssignableFrom(type)
               || (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>));
    }
}