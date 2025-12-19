using System.Text.Json.Serialization;
using Sonar.Rules.Helpers;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class EventDetails(PropertyMappings propertyMappings, EventTitles eventTitles, Details details)
{
    public PropertyMappings PropertyMappings { get; } = propertyMappings;
    public EventTitles EventTitles { get; } = eventTitles;
    public Details Details { get; } = details;

    internal static readonly Lazy<EventDetails> Instance = new(() => new EventDetails(ConfigHelper.GetPropertyMappings(), ConfigHelper.GetEventTitles(), ConfigHelper.GetDetails()), LazyThreadSafetyMode.ExecutionAndPublication);
}