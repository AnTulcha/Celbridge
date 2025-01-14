namespace Celbridge.Forms;

/// <summary>
/// A text block form element.
/// </summary>
public interface ITextBlockElement : IFormElement
{
    /// <summary>
    /// The text to display in the text block.
    /// </summary>
    public string Text { get; set; }
}
