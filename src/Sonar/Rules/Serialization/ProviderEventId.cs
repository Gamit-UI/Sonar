using System.Text.Json.Serialization;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
internal sealed class ProviderEventId(string? provider, string? eventId) : IEquatable<ProviderEventId>
{
    public string Provider { get; } = provider?.Trim() ?? string.Empty;
    public string EventId { get; } = eventId?.Trim() ?? string.Empty;

    public bool Equals(ProviderEventId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Provider == other.Provider && EventId == other.EventId;
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
        return HashCode.Combine(Provider, EventId);
    }

    public override string ToString()
    {
        return $"{Provider}:{EventId}";
    }
}