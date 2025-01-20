namespace Celbridge.Entities;

/// <summary>
/// Defines an editor for editing an entity component.
/// </summary>
public interface IComponentEditor
{
    /// <summary>
    /// Path to the JSON confuration file for this component.
    /// </summary>
    string ComponentConfigPath { get; }

    /// <summary>
    /// Initialize the component editor with the component proxy that it edits.
    /// </summary>
    IComponentProxy? Component { get; set; }

    /// <summary>
    /// Returns the type of editor view to instantiate for this component.
    /// </summary>
    Type EditorViewType { get; }

    Result<object> GetProperty(string name);
    Result<bool> SetProperty(string name, object value);
}
