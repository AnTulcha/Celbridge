using Celbridge.Forms;

namespace Celbridge.UserInterface.Services.Forms;

public class FormBuilder : IFormBuilder
{
    public Result<FormInstance> Build(IForm form)
    {
        var formPanel = form.Panel;
        var uiPanel = BuildUIPanel(formPanel);
        if (uiPanel is null)
        {
            return Result<FormInstance>.Fail($"Failed to build form panel: '{formPanel}'");
        }

        foreach (var formChild in formPanel.Children)
        {
            var uiElement = BuildUIElement(formChild);
            if (uiElement is null)
            {
                return Result<FormInstance>.Fail($"Failed to build form element: {formChild}");
            }
            uiPanel.Children.Add(uiElement);
        }

        var formInstance = new FormInstance(form, uiPanel);

        return Result<FormInstance>.Ok(formInstance);
    }

    private Panel? BuildUIPanel(IFormPanel formPanel)
    {
        if (formPanel is IStackPanelElement)
        {
            var stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            return stackPanel;
        }

        return null;
    }

    private UIElement? BuildUIElement(IFormElement formElement)
    {
        if (formElement is ITextBlockElement formTextBlock)
        {
            var textBlock = new TextBlock();
            textBlock.Text = formTextBlock.Text;
            return textBlock;
        }

        return null;
    }
}
