using Celbridge.Forms;

namespace Celbridge.UserInterface.Services.Forms;

public class FormFactory : IFormFactory
{
    private readonly IServiceProvider _serviceProvider;

    public FormFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IForm CreateVerticalForm()
    {
        // Todo: Set orientation to vertical
        var form = _serviceProvider.GetRequiredService<IForm>();
        form.Panel = CreateStackPanel();

        return form;
    }

    public IForm CreateHorizontalForm()
    {
        // Todo: Set orientation to horizontal
        var form = _serviceProvider.GetRequiredService<IForm>();
        form.Panel = CreateStackPanel();

        return form;
    }

    public ITextBlockElement CreateTextBlock()
    {
        var element = _serviceProvider.GetRequiredService<ITextBlockElement>();
        return element;
    }

    public IStackPanelElement CreateStackPanel()
    {
        var panel = _serviceProvider.GetRequiredService<IStackPanelElement>();
        return panel;
    }
}
