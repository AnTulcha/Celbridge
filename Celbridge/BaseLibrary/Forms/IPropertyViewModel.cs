namespace Celbridge.Forms;

/// <summary>
/// A view model that binds a form UI element to a form property via a form data provider.
/// </summary>
public interface IPropertyViewModel
{
    /// <summary>
    /// The name of the member variable on the view model class to bind to.
    /// </summary>
    string BoundPropertyName { get; }

    /// <summary>
    /// The form data provider that provides access to the form data.
    /// </summary>
    IFormDataProvider? FormDataProvider { get; set; }

    /// <summary>
    /// The property path to bind to via the form data provider.
    /// </summary>
    string PropertyPath { get; set; }

    /// <summary>
    /// Initializes the view model with the form data provider.
    /// </summary>
    Result Initialize();

    /// <summary>
    /// A callback called when the view is unloaded.
    /// </summary>
    void OnViewUnloaded();
}
