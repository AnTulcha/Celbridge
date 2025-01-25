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
/// A service for registering and building forms.
/// Forms are UI elements used to edit the properties of an object, such as entity component.
/// </summary>
public interface IFormService
{
    /// <summary>
    /// Registers a form with using a JSON form configuration.
    /// If the scope is set to Workspace, the form will be unregistered when the workspace is closed.
    /// This avoids exposing the forms from previously loaded projects to the current project.
    /// </summary>
    Result RegisterForm(string formName, string formConfigJSON, FormScope scope);

    /// <summary>
    /// Creates an instance of a previously registered form.
    /// The form data provider is used to populate the form and resolve bindings.
    /// </summary>
    Result<object> CreateRegisteredForm(string formName, IFormDataProvider formDataProvider);

    /// <summary>
    /// Creates an instance of a form based on a JSON form configuration.
    /// Property bindings are not supported.
    /// The form layout specified the containter control to use for the form.
    /// </summary>
    Result<object> CreateForm(string formName, string formConfigJSON, FormLayout formLayout);
}
