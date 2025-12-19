using Sonar.Rules.Serialization;

namespace Sonar.Tests.Detection.Rules;

public interface ITestData
{
    string YamlRule { get; }
    IList<WinEvent> WinEvents { get; }
    bool Match { get; }
    string? Details { get; }
}