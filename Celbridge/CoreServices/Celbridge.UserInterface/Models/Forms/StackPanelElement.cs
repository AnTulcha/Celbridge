using Celbridge.Forms;

namespace Celbridge.UserInterface.Models.Forms;

public class StackPanelElement : IStackPanelElement
{
    public IList<IFormElement> Children { get; } = new List<IFormElement>();
    public FormOrientation Orientation { get; set; }
}
