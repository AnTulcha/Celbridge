using Celbridge.Entities;
using Celbridge.Forms;

namespace Celbridge.Screenplay.Components;

public class EmptyComponent : IComponentDescriptor
{
    private readonly IFormFactory _formFactory;

    public EmptyComponent(IFormFactory formFactory)
    {
        _formFactory = formFactory;
    }

    public string SchemaJson => """
    {
        "type": "object",
        "additionalProperties": false,

        "attributes": {
        "allowMultipleComponents": true
        },

        "properties": {
        "_componentType": {
            "type": "string",
            "const": "Empty#1"
        },
        "comment": {
            "type": "string"
        }
        },

        "required": [
        "_componentType",
        "comment"
        ],

        "prototype": {
        "comment": ""
        }
    }
    """;

    public Result<IForm> CreateDetailForm(IComponentProxy component)
    {
        var textBlockA = _formFactory.CreateTextBlock();
        textBlockA.Text = "Hello, World!";

        var textBlockB = _formFactory.CreateTextBlock();
        textBlockB.Text = "1";

        var textBlockC = _formFactory.CreateTextBlock();
        textBlockC.Text = "2";

        var form = _formFactory.CreateForm(FormOrientation.Vertical);
        form.Panel.Children.Add(textBlockA);

        var childPanel = _formFactory.CreateStackPanel(FormOrientation.Horizontal);
        childPanel.Children.Add(textBlockB);
        childPanel.Children.Add(textBlockC);

        form.Panel.Children.Add(childPanel);

        return Result<IForm>.Ok(form);
    }
}
