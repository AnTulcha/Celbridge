namespace Celbridge.Forms;

/// <summary>
/// Provides data for a form, with support for change notification.
/// </summary>
public interface IFormDataProvider
{
    /// <summary>
    /// Callback called when the form UI is unloaded.
    /// </summary>
    void OnFormUnloaded();

    /// <summary>
    /// Event that is fired when an edited property value changes.
    /// </summary>
    event Action<string>? PropertyChanged;

    /// <summary>
    /// Gets the property at the specified path.
    /// </summary>
    Result<string> GetProperty(string propertyPath);

    /// <summary>
    /// Sets the property at the specified path.
    /// </summary>
    Result SetProperty(string propertyPath, string newValue, bool insert);
}
