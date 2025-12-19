using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sonar.Detections;
using Sonar.Rules.Helpers;

namespace Sonar.Helpers;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, MitreComponent>))]
[JsonSerializable(typeof(IEnumerable<DetectionExport>))]
internal partial class SerializationContext : JsonSerializerContext
{
    static SerializationContext()
    {
        Default = new SerializationContext(CreateJsonSerializerOptions(Default));
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions(SerializationContext defaultContext)
    {
        return new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = defaultContext.Options.WriteIndented
        };
    }
}