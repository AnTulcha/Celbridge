namespace Celbridge.Entities;

/// <summary>
/// Represents an instance of a component.
/// Provides convenient access to the component's data and configuration info.
/// </summary>
public interface IComponentProxy
{
    /// <summary>
    /// Returns true if the component proxy reference is still valid.
    /// Proxies are invalidated when any structural changes are made to the entity.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Returns the resource for the entity that contains the component.
    /// </summary>
    ResourceKey Resource { get; }

    /// <summary>
    /// Returns the index of the component.
    /// </summary>
    int ComponentIndex { get; }

    /// <summary>
    /// Returns the schema of the component.
    /// </summary>
    ComponentSchema Schema { get; }

    /// <summary>
    /// The validation status of the component.
    /// </summary>
    ComponentStatus Status { get; }

    /// <summary>
    /// The description of the component displayed in the editor UI.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The tooltip displayed in the editor UI.
    /// </summary>
    string Tooltip { get; }

    /// <summary>
    /// Set all annotation properties.
    /// </summary>
    void SetAnnotation(ComponentStatus Status, string Description, string Tooltip);

    /// <summary>
    /// Convenience method to get a string property with minimal boilerplate.
    /// Returns the default value if the property cannot be found.
    /// </summary>
    string GetString(string propertyPath, string defaultValue = "");
}
