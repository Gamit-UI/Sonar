using System.Text.Json.Serialization;
using Sonar.Events.Extensions;

namespace Sonar.Rules.Serialization;

[method: JsonConstructor]
public class WinEvent(IDictionary<string, string> system, IDictionary<string, string> eventData)
{
    public IDictionary<string, string> System { get; } = system;

    public IDictionary<string, string> EventData { get; } = eventData;

    [JsonIgnore]
    public ushort EventId => this.GetEventId();
    
    [JsonIgnore]
    public DateTime SystemTime => this.GetSystemTime();

    [JsonIgnore]
    public string ProviderName => this.GetProviderName();
    
    [JsonIgnore]
    public Guid ProviderGuid => this.GetProviderGuid();
    
    [JsonIgnore]
    public string Computer => this.GetComputer();
    
    [JsonIgnore]
    public ushort ProcessId => this.GetProcessId();
    
    [JsonIgnore]
    public ushort ThreadId => this.GetThreadId();
}