using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class PropertyMapping(IDictionary<string, Dictionary<string, string>> propertyValueByNames, IEnumerable<string> propertiesFromHexToDecimal)
{
    public IDictionary<string, Dictionary<string, string>> PropertyValueByNames { get; } = propertyValueByNames;
    public IEnumerable<string> PropertiesFromHexToDecimal { get; } = propertiesFromHexToDecimal;
}