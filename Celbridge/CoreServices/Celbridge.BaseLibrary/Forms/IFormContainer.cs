namespace Celbridge.Forms;

/// <summary>
/// A form element that can contain other form elements.
/// </summary>
public interface IFormContainer : IFormElement
{
    /// <summary>
    /// The child elements of this container.
    /// </summary>
    IList<IFormElement> Children { get; }
}
