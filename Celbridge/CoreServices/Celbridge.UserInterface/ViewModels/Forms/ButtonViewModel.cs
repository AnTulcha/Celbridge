using Celbridge.Forms;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class ButtonViewModel : ElementViewModel, IButtonViewModel
{
    public string ButtonId = string.Empty;

    public void OnButtonClicked()
    {
        FormDataProvider?.OnButtonClicked(ButtonId);
    }
}
