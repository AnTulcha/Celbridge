namespace Celbridge.Inspector.Models;

public class Form : IForm
{
    public object FormUIElement { get; }

    public Form(object formUIElement)
    {
        FormUIElement = formUIElement;
    }
}
