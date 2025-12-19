using Sonar.Rules.Serialization;

namespace Sonar.Rules.Helpers;

internal delegate bool RuleFilter(in RuleMetadata metadata, in ISet<ushort> eventIds);