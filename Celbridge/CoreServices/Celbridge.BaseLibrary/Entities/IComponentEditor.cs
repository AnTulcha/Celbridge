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
}
