namespace Celbridge.Forms;

/// <summary>
/// A declarative representation used to define the structure of a form.
/// Form instances are constructed via the FormBuilder service using this definition.
/// </summary>
public interface IForm
{
    /// <summary>
    /// The root container of the form.
    /// </summary>
    IFormContainer Container { get; }
}
