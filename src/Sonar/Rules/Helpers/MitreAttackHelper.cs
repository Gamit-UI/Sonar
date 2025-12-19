using System.Text.Json;
using System.Text.RegularExpressions;
using Sonar.Extensions;

namespace Sonar.Rules.Helpers;

internal static partial class MitreAttackHelper
{
    private static readonly Dictionary<string, MitreComponent> MitreById;
    private static readonly Dictionary<string, MitreComponent> TacticByTitle;

    private static int Order(string input)
    {
        if (TechniqueRegex().IsMatch(input)) return -1;
        return 0;
    }
    
    public static IEnumerable<MitreComponent> GetTactics(IEnumerable<string> input)
    {
        var regex = AttackRegex();
        foreach (var item in input.OrderBy(Order))
        {
            var match = regex.Match(item); 
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = match.Groups[2].Value;
                }
                
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = match.Groups[3].Value;
                }

                if (string.IsNullOrWhiteSpace(value)) continue;
                if (MitreById.TryGetValue(value, out var component))
                {
                    if (TacticByTitle.TryGetValue(component.Tactic, out var mitre))
                    {
                        yield return mitre;
                    }
                }
                else
                {
                    var sentence = value.Replace("-", " ");
                    foreach (var mitreComponent in MitreById.Values)
                    {
                        if (mitreComponent.Tactic.Equals(sentence, StringComparison.OrdinalIgnoreCase) &&
                            mitreComponent.SubTechnique.Equals("-") &&
                            mitreComponent.Technique.Equals("-"))
                        {
                            if (TacticByTitle.TryGetValue(mitreComponent.Tactic, out var mitre))
                            {
                                yield return mitre;
                            }
                        }
                        else if (mitreComponent.Technique.Equals(sentence, StringComparison.OrdinalIgnoreCase))
                        {
                            if (TacticByTitle.TryGetValue(mitreComponent.Tactic, out var mitre))
                            {
                                yield return mitre;
                            }
                        }
                        else if (mitreComponent.SubTechnique.Equals(sentence, StringComparison.OrdinalIgnoreCase))
                        {
                            if (TacticByTitle.TryGetValue(mitreComponent.Tactic, out var mitre))
                            {
                                yield return mitre;
                            }
                        }
                    }
                }
            }
        }
    }

    static MitreAttackHelper()
    {
        using var stream = typeof(MitreAttackHelper).Assembly.ReadFromEmbeddedResource("mitre-attack.json");
        using var reader = new StreamReader(stream);
        var elements = JsonSerializer.Deserialize(reader.ReadToEnd(), Sonar.Helpers.SerializationContext.Default.DictionaryStringMitreComponent) ?? new Dictionary<string, MitreComponent>();
        MitreById = elements.ToDictionary(kvp => kvp.Key, kvp => kvp.Value with { Id = kvp.Key }, StringComparer.OrdinalIgnoreCase);
        TacticByTitle = MitreById.Where(mitre => !mitre.Value.Tactic.Equals("-") && mitre.Value.Technique.Equals("-") && mitre.Value.SubTechnique.Equals("-")).GroupBy(kvp => kvp.Value.Tactic).ToDictionary(group => group.Key, group => group.Select(kvp => kvp.Value).DistinctBy(mitre => mitre.Id).Single());
    }
    
    [GeneratedRegex("^attack\\.(t\\d+\\.?\\d+)?$|^attack\\.(.*)?$|Rule: Attack=(t\\d+\\.?\\d+)?", RegexOptions.IgnoreCase)]
    private static partial Regex AttackRegex();
    
    [GeneratedRegex("^attack\\.(t\\d+\\.?\\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex TechniqueRegex();
}