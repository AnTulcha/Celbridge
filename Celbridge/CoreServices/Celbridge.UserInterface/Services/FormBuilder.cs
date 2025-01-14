using Celbridge.Forms;

namespace Celbridge.UserInterface.Services;

public class FormBuilder : IFormBuilder
{
    public Result<FormInstance> Build(IForm form)
    {
        var formContainer = form.Container;
        var panel = BuildFormContainer(formContainer);
        if (panel is null)
        {
            return Result<FormInstance>.Fail($"Failed to build form panel");
        }

        foreach (var child in formContainer.Children)
        {
            var element = BuildFormElement(child);
            if (element is null)
            {
                return Result<FormInstance>.Fail($"Failed to build form element");
            }
            panel.Children.Add(element);
        }

        var formInstance = new FormInstance(form, panel);

        return Result<FormInstance>.Ok(formInstance);
    }

    private Panel? BuildFormContainer(IFormContainer formContainer)
    {
        if (formContainer is IStackPanelContainer)
        {
            var stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;
            return stackPanel;
        }

        return null;
    }

    private UIElement? BuildFormElement(IFormElement formElement)
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
