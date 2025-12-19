using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Sonar.Events.Parsers;
using Sonar.Rules.Serialization;
using TurboXml;

namespace Sonar.Events.Extensions;

internal static class WinEventExtensions
{
    public const string EventIdKey = "EventID";
    public const string ChannelKey = "Channel";
    public const string SystemTimeKey = "SystemTime";
    public const string ProviderNameKey = "Name";
    public const string ProviderGuidKey = "Guid";
    public const string ComputerKey = "Computer";
    private const string ProcessIdKey = "ProcessId";
    private const string ThreadIdKey = "ThreadId";
    
    public static readonly ISet<string> SystemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        EventIdKey,
        ChannelKey,
        ProviderNameKey,
        ProviderGuidKey,
        SystemTimeKey,
        ComputerKey,
        ProcessIdKey,
        ThreadIdKey
    };
    
    extension(WinEvent winEvent)
    {
        public ISet<ChannelEventId> GetChannelEventIds()
        {
            var channelEventIds = new HashSet<ChannelEventId>();
            var channel = winEvent.GetChannelName();
            var eventId = winEvent.GetEventId();

            channelEventIds.Add(new ChannelEventId(channel, eventId.ToString()));
            return channelEventIds;
        }

        public ISet<ProviderEventId> GetProviderEventIds()
        {
            var providerEventIds = new HashSet<ProviderEventId>();
            var provider = winEvent.GetProviderName();
            var eventId = winEvent.GetEventId();

            providerEventIds.Add(new ProviderEventId(provider, eventId.ToString()));
            return providerEventIds;
        }

        public ushort GetEventId()
        {
            if (winEvent.System.TryGetValue(EventIdKey, out var eventId))
            {
                return ushort.Parse(eventId);
            }

            return 0;
        }

        public ushort GetProcessId()
        {
            if (winEvent.System.TryGetValue(ProcessIdKey, out var processId))
            {
                return ushort.Parse(processId);
            }

            return 0;
        }

        public ushort GetThreadId()
        {
            if (winEvent.System.TryGetValue(ThreadIdKey, out var threadId))
            {
                return ushort.Parse(threadId);
            }

            return 0;
        }

        public string GetProviderName()
        {
            if (winEvent.System.TryGetValue(ProviderNameKey, out var providerName))
            {
                return providerName;
            }

            return string.Empty;
        }

        public string GetComputer()
        {
            if (winEvent.System.TryGetValue(ComputerKey, out var computerName))
            {
                return computerName;
            }

            return string.Empty;
        }

        public DateTime GetSystemTime()
        {
            if (winEvent.System.TryGetValue(SystemTimeKey, out var systemTime))
            {
                return DateTime.Parse(systemTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            }

            return DateTime.UtcNow;
        }

        private string GetChannelName()
        {
            if (winEvent.System.TryGetValue(ChannelKey, out var channel))
            {
                return channel;
            }

            return string.Empty;
        }

        public Guid GetProviderGuid()
        {
            if (winEvent.System.TryGetValue(ProviderGuidKey, out var providerGuid))
            {
                return Guid.Parse(providerGuid);
            }

            return Guid.Empty;
        }
    }
    
    extension(EventRecord record)
    {
        private WinEvent BuildWinEvent(string channel, Dictionary<string, string> eventData)
        {
            var system = new Dictionary<string, string>(capacity: 8, StringComparer.OrdinalIgnoreCase)
            {
                { EventIdKey, record.Id.ToString() },
                { ChannelKey, channel },
                { ProviderNameKey, record.ProviderName },
                { ProviderGuidKey, record.ProviderId == null ? Guid.Empty.ToString() : record.ProviderId.Value.ToString() },
                { SystemTimeKey, record.TimeCreated == null ? DateTime.UtcNow.ToString("O") : record.TimeCreated.Value.ToUniversalTime().ToString("O") },
                { ComputerKey, record.MachineName },
                { ProcessIdKey, !record.ProcessId.HasValue ? "0" : record.ProcessId.Value.ToString() },
                { ThreadIdKey, !record.ThreadId.HasValue ? "0" : record.ThreadId.Value.ToString() }
            };

            return new WinEvent(system, eventData);
        }

        public bool TryGetWinEvent([MaybeNullWhen(false)] out WinEvent winEvent, string? server = null)
        {
            winEvent = null;
            if (record.RecordId is null || record.ProviderId == null || record.Version == null) return false;
            var key = new EventRecordKey(record.Id, record.ProviderId.Value, record.Version.Value);
            if (PropertiesByEventKey.TryGetValue(key, out var properties))
            {
                var data = new Dictionary<string, string>(capacity: properties.Length, StringComparer.OrdinalIgnoreCase);
                winEvent = record.BuildWinEvent(record.LogName, data);
                for (var i = 0; i < properties.Length; i++)
                {
                    var prop = properties[i];
                    var property = record.Properties[i];
                    var value = property?.Value?.ToString();
                    if (value is null) break;
                    if (prop.IsHex)
                    {
                        if (short.TryParse(value, out var shortValue))
                        {
                            data.Add(prop.PropertyName, shortValue.ToString("x"));
                        }
                        else if (int.TryParse(value, out var intValue))
                        {
                            data.Add(prop.PropertyName, intValue.ToString("x"));
                        }
                        else if (long.TryParse(value, out var longValue))
                        {
                            data.Add(prop.PropertyName, longValue.ToString("x"));
                        }
                    }
                    else
                    {
                        data.Add(prop.PropertyName, value);
                    }
                }
            }
            else
            {
                var xml = record.ToXml();
                var parser = new EventLogXmlParser();
                XmlParser.Parse(xml, ref parser);
                var data = new Dictionary<string, string>(capacity: parser.Properties.Count, StringComparer.OrdinalIgnoreCase);
                winEvent = record.BuildWinEvent(record.LogName, data);
                foreach (var pair in parser.Properties)
                {
                    data.Add(pair.Key, pair.Value);
                }

                PropertiesByEventKey.TryAdd(key, parser.Properties.Select(kvp => kvp.Key).Select(propertyKey =>
                {
                    if (data.TryGetValue(propertyKey, out var value))
                    {
                        if (value.StartsWith("0x"))
                        {
                            return new Property(propertyKey, IsHex: true);
                        }
                    }
                
                    return new Property(propertyKey, IsHex: false);
                }).ToArray());
            }

            if (server is not null)
            {
                winEvent.System[ComputerKey] = server;
            }
        
            return true;
        }
    }

    private readonly struct EventRecordKey(int eventId, Guid providerGuid, byte version) : IEquatable<EventRecordKey>
    {
        private int EventId { get; } = eventId;
        private Guid ProviderGuid { get; } = providerGuid;
        private byte Version { get; } = version;

        public bool Equals(EventRecordKey other)
        {
            return EventId == other.EventId && ProviderGuid.Equals(other.ProviderGuid) && Version == other.Version;
        }

        public override bool Equals(object? obj)
        {
            return obj is EventRecordKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EventId, ProviderGuid, Version);
        }
    }

    private sealed record Property(string PropertyName, bool IsHex);
    private static readonly ConcurrentDictionary<EventRecordKey, Property[]> PropertiesByEventKey = new();
}