namespace Celbridge.Forms;

/// <summary>
/// A form element that binds a form button UI element to a form property via a form data provider.
/// </summary>
public interface IButtonElement
{
    /// <summary>
    /// Called when the use clicks the button.
    /// </summary>
    void OnButtonClicked();

    /// <summary>
    /// Called when the view is unloaded.
    /// </summary>
    void OnViewUnloaded();
}
