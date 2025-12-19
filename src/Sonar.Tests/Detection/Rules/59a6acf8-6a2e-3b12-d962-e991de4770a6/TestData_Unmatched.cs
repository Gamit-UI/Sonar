using Sonar.Events.Extensions;
using Sonar.Rules.Serialization;

namespace Sonar.Tests.Detection.Rules._59a6acf8_6a2e_3b12_d962_e991de4770a6;

public class TestData_Unmatched : TestData
{
    public TestData_Unmatched() : base(_59a6acf8_6a2e_3b12_d962_e991de4770a6.YamlRule.Yaml)
    {
        var system = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { WinEventExtensions.EventIdKey, "4688" },
            { WinEventExtensions.ChannelKey, "Security" },
            { WinEventExtensions.ProviderNameKey, "Microsoft-Windows-Security-Auditing" },
            { WinEventExtensions.ProviderGuidKey, "54849625-5478-4994-A5BA-3E3B0328C30D" },
            { WinEventExtensions.SystemTimeKey, "2025-01-29T14:45:54.020972Z" },
            { WinEventExtensions.ComputerKey, "LOCAL" }
        };

        var eventData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NewProcessName", "C:\\WINDOWS\\system32\\svchost.exe" },
            { "CommandLine", "-k netsvcs -p -s wuauserv" },
            { "ParentProcessName", "C:\\WINDOWS\\system32\\svchost.exe" }
        };
            
        Add(new WinEvent(system, eventData));
        Match = false;
    }
}