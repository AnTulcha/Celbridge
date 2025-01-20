namespace Celbridge.Forms;

/// <summary>
/// A service that constructs form UI elements based on a JSON definition.
/// </summary>
public interface IFormBuilder
{
    /// <summary>
    /// Constructs a form UI element based on a JSON definition.
    /// </summary>
    Result<object> BuildForm(string fromConfigJson);
}
