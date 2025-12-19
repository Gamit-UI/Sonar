using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace Sonar.Rules.Serialization.Yaml;

[YamlSerializable]
internal sealed class YamlRule
{
    [Required]
    [YamlMember(Alias = "author")]
    public string Author { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "date")]
    public string Date { get; set; } = string.Empty;

    [YamlMember(Alias = "modified")]
    public string Modified { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "details")]
    public string Details { get; set; } = string.Empty;

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "level")]
    public string Level { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "status")]
    public string Status { get; set; } = string.Empty;

    [Required]
    [YamlMember(Alias = "logsource")]
    public Dictionary<string, object> LogSources { get; set; } = new();

    [Required]
    [YamlMember(Alias = "detection")]
    public Dictionary<string, object> Detections { get; set; } = new();
    
    [YamlMember(Alias = "correlation")]
    public Dictionary<string, object> Correlations { get; set; } = new();

    [Required]
    [YamlMember(Alias = "falsepositives")]
    public List<string> FalsePositives { get; set; } = new();

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = new();

    [YamlMember(Alias = "references")]
    public List<string> References { get; set; } = new();

    [Required]
    [YamlMember(Alias = "ruletype")]
    public string Ruletype { get; set; } = string.Empty;
    
    [YamlMember(Alias = "rulefile")]
    public string Rulefile { get; set; } = string.Empty;

    public string GetLink()
    {
        return string.Concat("https://github.com/Yamato-Security/hayabusa-rules/blob/main/", Rulefile.Replace("/home/runner/work/hayabusa-encoded-rules/hayabusa-encoded-rules/rules/", string.Empty));
    }
}