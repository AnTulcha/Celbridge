namespace Celbridge.Inspector;

/// <summary>
/// The inspector service provides functionality to support the inspector panel in the workspace UI.
/// </summary>
public interface IInspectorService
{
    /// <summary>
    /// The current resource being inspected.
    /// </summary>
    public ResourceKey InspectedResource { get; }

    /// <summary>
    /// The index of the current component being inspected.
    /// -1 if no component is currently being inspected.
    /// </summary>
    public int InspectedComponentIndex { get; }

    /// <summary>
    /// Returns the inspector panel view.
    /// </summary>
    public IInspectorPanel InspectorPanel { get; }

    /// <summary>
    /// Returns the factory used to create inspector UI elements. 
    /// </summary>
    public IInspectorFactory InspectorFactory { get; }
}
