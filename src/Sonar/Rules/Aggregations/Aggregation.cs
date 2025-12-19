namespace Sonar.Rules.Aggregations;

internal enum Selection
{
    One,
    All
}

internal record Aggregation(string? Condition, string? Property, string[] Dimensions, string Operator, string Value);
internal record OneOrAll(Selection Selection, string DetectionName);
