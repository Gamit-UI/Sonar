using System.Diagnostics.CodeAnalysis;
using Sonar.Rules.Helpers;

namespace Sonar.Rules.Extensions;

internal static class RuleExtensions
{
    public static bool TryGetMitreTactic(this RuleBase rule, [MaybeNullWhen(false)] out string name, [MaybeNullWhen(false)] out string link)
    {
        name = null;
        link = null;
        var tactics = MitreAttackHelper.GetTactics(rule.Metadata.Tags).ToList();
        if (tactics.Any())
        {
            var mitre = tactics.First();
            name = mitre.Tactic;
            link = $"https://attack.mitre.org/tactics/{mitre.Id}";
            return true;
        }

        return false;
    }
}