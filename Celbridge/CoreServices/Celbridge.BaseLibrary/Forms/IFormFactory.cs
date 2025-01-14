namespace Celbridge.Forms;

/// <summary>
/// A service that creates forms and form elements.
/// </summary>
public interface IFormFactory
{
    /// <summary>
    /// Creates a form with a vertical layout.
    /// </summary>
    IForm CreateVerticalForm();

    /// <summary>
    /// Creates a form with a horizontal layout.
    /// </summary>
    IForm CreateHorizontalForm();

    /// <summary>
    /// Creates a text block form element.
    /// </summary>
    ITextBlockElement CreateTextBlock();
}
