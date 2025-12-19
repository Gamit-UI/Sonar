using System.Diagnostics.CodeAnalysis;
using ConcurrentCollections;
using Sonar.Setup.Services.Options;

namespace Sonar.Rules.Stores;

internal interface IRuleStore
{
    ValueTask<int> InitializeAsync(SetupOptions setupOptions, IProgress<double> progress, Action onBeforeCount, Action<int> onCount, CancellationToken cancellationToken);
    bool TryGetStandardRules(ushort id, [MaybeNullWhen(false)] out ConcurrentHashSet<StandardRule> rules);
    bool TryGetAggregationRules(ushort id, [MaybeNullWhen(false)] out ConcurrentHashSet<AggregationRule> rules);
    bool TryGetLink(string ruleTitle, [MaybeNullWhen(false)] out string link);
    bool TryGetTactic(string ruleTitle, [MaybeNullWhen(false)] out string tactic, [MaybeNullWhen(false)] out string link);
    IEnumerable<string> GetRuleTitles();
}