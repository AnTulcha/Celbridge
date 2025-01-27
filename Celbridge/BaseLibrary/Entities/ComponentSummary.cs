namespace Celbridge.Entities;

/// <summary>
/// Describes how to display a component summary row in the inspector component list.
/// </summary>
public record ComponentSummary(int IndentLevel, string ComponentTypeColor, ComponentStatus Status, string SummaryText);
