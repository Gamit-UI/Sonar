using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Sonar.Rules;
using Sonar.Rules.Helpers;
using Sonar.Rules.Serialization;

namespace Sonar.Tests;

internal static class Helper
{
    private static readonly Aliases Aliases = ConfigHelper.GetAliases();
    private static readonly Details Details = ConfigHelper.GetDetails();
    private static readonly ChannelAbbrevations Channels = ConfigHelper.GetChannelAbbreviations();
    private static readonly ProviderAbbrevations Providers = ConfigHelper.GetProviderAbbreviations();
    
    public static bool TryGetRule(string yamlRule, [MaybeNullWhen(false)] out RuleBase rule, [MaybeNullWhen(false)] out ISet<string> properties, [MaybeNullWhen(true)] out string error)
    {
        return RuleHelper.TryDeserialize(NullLogger.Instance, yamlRule, Aliases, Details, Channels, Providers, out rule, out properties, out error);
    }
}