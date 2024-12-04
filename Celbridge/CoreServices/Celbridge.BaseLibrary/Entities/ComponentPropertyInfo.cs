namespace Celbridge.Entities;

/// <summary>
/// Describes a component property, its type and attributes.
/// </summary>
public record ComponentPropertyInfo(string PropertyName, string PropertyType, IReadOnlyDictionary<string, string> Attributes);

