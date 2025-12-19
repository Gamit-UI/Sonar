namespace Sonar.Tests.Detection;

[CollectionDefinition(nameof(RuleCollection))]
public sealed class RuleCollection : ICollectionFixture<RuleFixture>;