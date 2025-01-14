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
        textBlockB.Text = "It LIVESSSS!";

        var form = _formFactory.CreateForm();
        form.Container.Children.Add(textBlockA);
        form.Container.Children.Add(textBlockB);

        return Result<IForm>.Ok(form);
    }
}
