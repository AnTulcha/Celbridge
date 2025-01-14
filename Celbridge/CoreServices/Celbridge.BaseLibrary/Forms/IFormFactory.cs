namespace Celbridge.Forms;

/// <summary>
/// A service that creates forms and form elements.
/// </summary>
public interface IFormFactory
{
    /// <summary>
    /// Creates a form.
    /// </summary>
    IForm CreateForm();

    /// <summary>
    /// Creates a text block form element.
    /// </summary>
    ITextBlockElement CreateTextBlock();
}
