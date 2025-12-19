using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class ChannelEventId(string? channel, string? eventId) : IEquatable<ChannelEventId>
{
    public string Channel { get; } = channel?.Trim() ?? string.Empty;
    public string EventId { get; } = eventId?.Trim() ?? string.Empty;

    public bool Equals(ChannelEventId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Channel == other.Channel && EventId == other.EventId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ChannelEventId)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Channel, EventId);
    }

    public override string ToString()
    {
        return $"{Channel}:{EventId}";
    }
}