using Sonar.Rules.Helpers;

namespace Sonar.Setup.Services.Options;

internal sealed record SetupOptions(bool Interactive, bool Online, RuleFilter Filter);