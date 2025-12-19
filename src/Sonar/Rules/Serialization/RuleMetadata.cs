using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

public readonly struct RuleMetadata(string id, string title, string date, string? modified, string author, string? details, string? description, string level, string status, string link, IEnumerable<string> tags, IEnumerable<string> references, IEnumerable<string> falsePositives, TimeSpan? correlationOrAggregationTimeSpan)
{
    [JsonConstructor]
    public RuleMetadata(string id, string title, string date, string? modified, string author, string? details, string? description, string level, string status, string link, TimeSpan? correlationOrAggregationTimeSpan) : this(id, title, date, modified, author, details, description, level, status, link, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), correlationOrAggregationTimeSpan)
    {
        
    }
    
    public string Id { get; } = id;
    public string Title { get; } = title;
    public string Date { get; } = date;
    public string? Modified { get; } = modified;
    public string Author { get; } = author;
    public string? Details { get; } = details;
    public string? Description { get; } = description;
    public string Level { get; } = level;
    public string Status { get; } = status;
    public string Link { get; } = link;
    public IEnumerable<string> Tags { get; } = tags;
    public IEnumerable<string> References { get; } = references;
    public IEnumerable<string> FalsePositives { get; } = falsePositives;
    public TimeSpan? CorrelationOrAggregationTimeSpan { get; } = correlationOrAggregationTimeSpan;
}