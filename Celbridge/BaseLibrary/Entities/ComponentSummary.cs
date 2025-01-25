namespace Celbridge.Entities;

/// <summary>
/// Describes how to display a component summary row in the inspector component list.
/// </summary>
public record ComponentSummary(int indentLevel, string componentTypeColor, ComponentStatus status, string summaryFormJSON);
