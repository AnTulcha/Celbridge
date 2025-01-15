namespace Celbridge.Forms;

/// <summary>
/// A service that creates forms and form elements.
/// </summary>
public interface IFormFactory
{
    /// <summary>
    /// Creates a form with a horizontal or vertical layout.
    /// </summary>
    IForm CreateForm(FormOrientation orientation);

    /// <summary>
    /// Creates a stack panel form element.
    /// </summary>
    /// <returns></returns>
    IStackPanelElement CreateStackPanel(FormOrientation orientation);

    /// <summary>
    /// Creates a text block form element.
    /// </summary>
    ITextBlockElement CreateTextBlock();
}
