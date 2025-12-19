using System.Text.RegularExpressions;
using Sonar.Events.Extensions;
using Sonar.Rules;
using Sonar.Rules.Serialization;

namespace Sonar.Detections;

internal static class DetectionDetailsResolver
{
    private static readonly Regex DetailValues = new("\\%(\\w+)\\%", RegexOptions.Compiled);
    private static readonly Regex AccessMaskMatch = new("(%%\\d{4})", RegexOptions.Compiled);
    private const string Space = " ";
    private const string AccessMask = "AccessMask";
    
    public static DetectionDetails Resolve(WinEvent winEvent, RuleMetadata ruleMetadata)
    {
        var eventTitle = string.Empty;
        var channelEventIds = winEvent.GetChannelEventIds();
        var providerEventIds = winEvent.GetProviderEventIds();
        foreach (var channelEventId in channelEventIds)
        {
            if (EventDetails.Instance.Value.EventTitles.Items.TryGetValue(channelEventId, out var title))
            {
                eventTitle = title;
                break;
            }
        }

        var details = string.Empty;
        if (!string.IsNullOrEmpty(ruleMetadata.Details))
        {
            details = FormatDetails(winEvent, channelEventIds, ruleMetadata.Details);
        }
        else
        {
            foreach (var providerEventId in providerEventIds)
            {
                if (EventDetails.Instance.Value.Details.Items.TryGetValue(providerEventId, out var eventDetail))
                {
                    details = FormatDetails(winEvent, channelEventIds, eventDetail);
                    break;
                }
            }
        }

        return new DetectionDetails(eventTitle, details, ruleMetadata, winEvent.GetSystemTime());
    }

    private static string FormatDetails(WinEvent winEvent, ISet<ChannelEventId> channelEventIds, string details)
    {
        var includedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 0;
        foreach (Match m in DetailValues.Matches(details))
        {
            var name = m.Groups[1].Value;
            var value = winEvent.GetValue(name);
            if (string.IsNullOrEmpty(value)) continue;
            var found = false;
            count++;
            foreach (var channelEventId in channelEventIds)
            {
                if (EventDetails.Instance.Value.PropertyMappings.Items.TryGetValue(channelEventId, out var propertyMapping))
                {
                    found = true;
                    if (propertyMapping.PropertyValueByNames.TryGetValue(name, out var values))
                    {
                        if (name.Equals(AccessMask, StringComparison.Ordinal))
                        {
                            var masks = new List<string>();
                            foreach (Match match in AccessMaskMatch.Matches(value))
                            {
                                if (values.TryGetValue(match.Value, out var accessMask))
                                {
                                    masks.Add(accessMask);
                                }
                            }

                            if (masks.Count > 0)
                            {
                                details = details.Replace(m.Value, string.Join(Space, masks));
                            }
                        }
                        else
                        {
                            if (values.TryGetValue(value, out var replacedValue))
                            {
                                details = details.Replace(m.Value, replacedValue);
                            }
                            else
                            {
                                details = details.Replace(m.Value, value);
                            }
                        }
                    }
                    else
                    {
                        details = details.Replace(m.Value, value);
                    }
                }
            }

            if (!found)
            {
                details = details.Replace(m.Value, value);
            }

            includedProperties.Add(name);
        }

        if (count < winEvent.EventData.Count)
        {
            var remainingProperties = winEvent.EventData.Where(kvp => !includedProperties.Contains(kvp.Key));
            return string.Concat(DetailValues.Replace(details, Constants.Unknown).Trim(), Constants.Separator, string.Join(Constants.Separator, remainingProperties.Select(data => $"{data.Key}: {(string.IsNullOrWhiteSpace(data.Value) ? Constants.Unknown : data.Value)}")));
        }

        return DetailValues.Replace(details, Constants.Unknown).Trim();
    }

    public static ISet<string> GetProperties(RuleMetadata ruleMetadata, ISet<ProviderEventId> providerEventIds)
    {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(ruleMetadata.Details))
        {
            foreach (Match m in DetailValues.Matches(ruleMetadata.Details))
            {
                properties.Add(m.Groups[1].Value);
            }
        }
        else
        {
            foreach (var providerEventId in providerEventIds)
            {
                if (EventDetails.Instance.Value.Details.Items.TryGetValue(providerEventId, out var eventDetail))
                {
                    foreach (Match m in DetailValues.Matches(eventDetail))
                    {
                        properties.Add(m.Groups[1].Value);
                    }
                }
            }
        }

        return properties;
    }
}