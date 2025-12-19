using Sonar.Rules.Serialization.Yaml.Converters;
using Sonar.Rules.Serialization.Yaml.Deserializers;
using Sonar.Rules.Serialization.Yaml.Extensions;
using Sonar.Rules.Serialization.Yaml.Resolvers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Sonar.Rules.Serialization.Yaml;

internal static class YamlParser
{
    private static readonly IDeserializer Deserializer;
    
    static YamlParser()
    {
        var context = new YamlContext();
        Deserializer = new StaticDeserializerBuilder(context)
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new DynamicObjectConverter())
            .WithNodeTypeResolver(new MappingNodeResolver(), ls => ls.InsteadOf<DefaultContainersNodeTypeResolver>())
            .WithNodeDeserializer(new ListsAcceptScalarDeserializer())
            .WithNodeDeserializer(new ForceEmptyListsOnDeserialization())
            .Build();
    }
    
    public static IEnumerable<T> DeserializeMany<T>(string yamlString)
    {
        using var sr = new StringReader(yamlString);
        foreach (var item in Deserializer.DeserializeMany<T>(sr).Where(item => item is not null))
        {
            yield return item;
        }
    }
    
    public static T Deserialize<T>(Stream stream)
    {
        using var sr = new StreamReader(stream);
        return Deserializer.Deserialize<T>(sr);
    }

    
    public static T Deserialize<T>(StringReader streamReader)
    {
        return Deserializer.Deserialize<T>(streamReader);
    }
}