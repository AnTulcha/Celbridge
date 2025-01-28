namespace Celbridge.Entities;

/// <summary>
/// Stores the annotations for the components in an entity.
/// </summary>
public interface IEntityAnnotation
{
    /// <summary>
    /// Initializes the annotation with the number of components in the entity.
    /// </summary>
    void Initialize(int count);

    /// <summary>
    /// Returns the number of component annotations.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Set a flag to indicate that this component has been recognised by the activity system.
    /// Unrecognized activities are automatically flagged as invalid.
    /// </summary>
    void SetIsRecognized(int componentIndex);

    /// <summary>
    /// Set the indent level for the component in the inspector.
    /// </summary>
    void SetIndent(int componentIndex, int indentLevel);

    /// <summary>
    /// Associate an error message with the component.
    /// </summary>
    void AddError(int componentIndex, ComponentError error);

    /// <summary>
    /// Returns the annotation data for the component.
    /// </summary>
    ComponentAnnotation GetAnnotation(int componentIndex);
}
