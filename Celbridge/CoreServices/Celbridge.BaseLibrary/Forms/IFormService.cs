namespace Celbridge.Forms;

/// <summary>
/// Describes when during the application lifecycle a form is available to create.
/// </summary>
public enum FormScope
{
    /// <summary>
    /// Form is available at any time during the full application lifecycle.
    /// </summary>
    Application,

    /// <summary>
    /// Form is available while the current workspace/project is loaded.
    /// </summary>
    Workspace
}

/// <summary>
/// A service for registering and building forms, defined in JSON.
/// </summary>
public interface IFormService
{
    /// <summary>
    /// Registers a form with using a JSON form definition.
    /// If the scope is set to Workspace, the form will be unregistered when the workspace is closed.
    /// This avoids exposing the forms from previously loaded projects to the current project.
    /// </summary>
    Result RegisterForm(string formName, string formJSON, FormScope scope);

    /// <summary>
    /// Creates a UI Element instance of the named form using the specified form data provider.
    /// </summary>
    Result<object> CreateForm(string formName, IFormDataProvider formDataProvider);
}
