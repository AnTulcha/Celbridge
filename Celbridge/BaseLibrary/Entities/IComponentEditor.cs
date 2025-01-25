using Celbridge.Forms;

namespace Celbridge.Entities;

/// <summary>
/// A component editor edits the properties of an entity component.
/// It implements IFormDataProvider, so it is typically used as the data provider for a form.
/// </summary>
public interface IComponentEditor : IFormDataProvider
{
    /// <summary>
    /// Path to the JSON confuration file for this component.
    /// </summary>
    string ComponentConfigPath { get; }

    /// <summary>
    /// Returns the component that the editor instance edits.
    /// </summary>
    IComponentProxy Component { get; }

    /// <summary>
    /// Initializes the component editor with the component to be edited.
    /// If observeComponentChanges is true then the editor will listen for changes to the component 
    /// and update the form accordingly.
    /// </summary>
    Result Initialize(IComponentProxy component, bool observeComponentChanges);

    /// <summary>
    /// Gets summary information for the edited component.
    /// </summary>
    Result<ComponentSummary> GetComponentSummary();
}
