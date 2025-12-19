using Sonar.Rules;
using TurboXml;

namespace Sonar.Events.Parsers;

internal struct EventLogXmlParser : IXmlReadHandler
{
    public readonly IDictionary<string, string> Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private string? propertyName;
    private string? propertyValue;
    private string? currentTag;

    private const string Name = "Name";
    private const string Data = "Data";
    
    public EventLogXmlParser()
    {
    }

    public void OnBeginTag(ReadOnlySpan<char> name, int line, int column)
    {
        currentTag = new string(name);
    }

    public void OnEndTag(ReadOnlySpan<char> name, int line, int column)
    {
        if (currentTag == Data && !string.IsNullOrEmpty(propertyName) && !string.IsNullOrEmpty(propertyValue))
        {
            if (!Properties.TryAdd(propertyName, propertyValue))
            {
                if (Properties.TryGetValue(propertyName, out var current))
                {
                    if (current.Contains(Constants.AbnormalSeparator))
                    {
                        var entries = current.Split(Constants.AbnormalSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
                        entries.Add(propertyValue);
                        Properties[propertyName] = string.Join(Constants.AbnormalSeparator, entries);
                    }
                    else
                    {
                        Properties[propertyName] = string.Join(Constants.AbnormalSeparator, new List<string> { current, propertyValue });
                    }
                }
            }
        }
    }

    public void OnAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int nameLine, int nameColumn, int valueLine, int valueColumn)
    {
        propertyName = Name.Equals(new string(name), StringComparison.OrdinalIgnoreCase) ? new string(value) : null;
    }
    
    public void OnText(ReadOnlySpan<char> text, int line, int column)
    {
        propertyValue = new string(text);
    }
}