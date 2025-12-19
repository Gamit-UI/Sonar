using Sonar.Rules.Serialization;

namespace Sonar.Tests.Detection.Rules;

public class RuleTestData<T> : TheoryData<string, IList<WinEvent>, bool, string?> where T : ITestData
{
    public RuleTestData()
    {
        var data = (T?)Activator.CreateInstance(typeof(T), []);
        if (data is null) return;
        Add(data.YamlRule, data.WinEvents, data.Match, data.Details);
    }
}