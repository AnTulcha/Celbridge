using Celbridge.Forms;

namespace Celbridge.UserInterface.Services;

public class FormFactory : IFormFactory
{
    private readonly IServiceProvider _serviceProvider;

    public FormFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IForm CreateForm()
    {
        var form = _serviceProvider.GetRequiredService<IForm>();
        return form;
    }

    public ITextBlockElement CreateTextBlock()
    {
        var form = _serviceProvider.GetRequiredService<ITextBlockElement>();
        return form;
    }
}
