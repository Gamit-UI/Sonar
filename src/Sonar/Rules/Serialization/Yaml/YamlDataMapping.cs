using YamlDotNet.Serialization;

namespace Sonar.Rules.Serialization.Yaml;

[YamlSerializable]
internal sealed class YamlDataMapping
{
    [YamlMember(Alias = "Title")]
    public string Title { get; set; } = string.Empty;
    
    [YamlMember(Alias = "Channel")]
    public string Channel { get; set; } = string.Empty;
    
    [YamlMember(Alias = "EventID")]
    public string EventId { get; set; } = string.Empty;

    [YamlMember(Alias = "RewriteFieldData")]
    public Dictionary<string, object> RewriteFieldData { get; set; } = new();
    
    [YamlMember(Alias = "HexToDecimal")]
    public List<string> HexToDecimal { get; set; } = new();
}