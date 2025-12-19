using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Sonar.Rules.Serialization.Yaml.Extensions;

internal static class YamlSerializerExtensions
{
    public static IEnumerable<TItem> DeserializeMany<TItem>(this IDeserializer deserializer, TextReader input)
    {
        var reader = new Parser(input);
        reader.Consume<StreamStart>();

        while (reader.TryConsume<DocumentStart>(out _))
        {
            var item = deserializer.Deserialize<TItem>(reader);
            yield return item;
            reader.TryConsume<DocumentEnd>(out _);
        }
    }
}