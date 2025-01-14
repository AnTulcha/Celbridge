using Celbridge.Forms;

namespace Celbridge.UserInterface.Models.Forms;

public class StackPanelContainer : IStackPanelContainer
{
    public IList<IFormElement> Children { get; } = new List<IFormElement>();
}
