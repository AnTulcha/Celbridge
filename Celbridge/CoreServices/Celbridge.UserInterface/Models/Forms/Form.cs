using Celbridge.Forms;

namespace Celbridge.UserInterface.Models.Forms;

public class Form : IForm
{
    public Form(IStackPanelContainer stackPanelContainer)
    {
        Container = stackPanelContainer;
    }

    public IFormContainer Container { get; set; }
}
