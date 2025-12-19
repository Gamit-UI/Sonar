using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Sonar.Rules.Serialization.Yaml.Resolvers;

internal sealed class MappingNodeResolver : INodeTypeResolver
{
    public bool Resolve(NodeEvent? nodeEvent, ref Type currentType)
    {
        if (currentType == typeof(object))
        {
            if (nodeEvent is MappingStart)
            {
                currentType = typeof(Dictionary<string, object>);
                return true;
            }
        }

        return false;
    }
}