namespace Celbridge.Forms;

/// <summary>
/// A stack panel form element.
/// </summary>
public interface IStackPanelElement : IFormPanel
{
    FormOrientation Orientation { get; set; }
}
