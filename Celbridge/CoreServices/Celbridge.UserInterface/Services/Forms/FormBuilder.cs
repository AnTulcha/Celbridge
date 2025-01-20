using System.Text.Json;
using Celbridge.Forms;

namespace Celbridge.UserInterface.Services.Forms;

public class FormBuilder : IFormBuilder
{
    public Result<object> BuildForm(string formConfigJson)
    {
        using (var document = JsonDocument.Parse(formConfigJson))
        {
            var rootArray = document.RootElement;

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var element in rootArray.EnumerateArray())
            {
                var createResult = CreateControlFromJson(element);
                if (createResult.IsFailure)
                {
                    return Result<object>.Fail(createResult.Error);
                }
                var control = createResult.Value;

                if (control != null)
                {
                    rootPanel.Children.Add(control);
                }
            }

            return Result<object>.Ok(rootPanel);
        }
    }

    private static Result<UIElement> CreateControlFromJson(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return Result<UIElement>.Fail("Form array element is not an object");
        }

        if (!element.TryGetProperty("elementType", out var elementTypeNode))
        {
            return Result<UIElement>.Fail("Form object does not contain an 'elementType' property");
        }

        var elementType = elementTypeNode.GetString();

        UIElement? uiElement = null;        
        switch (elementType)
        {
            case "StackPanel":
                uiElement = CreateStackPanel(element);
                break;

            case "TextBox":
                uiElement = CreateTextBox(element);
                break;

            case "Button":
                uiElement = CreateButton(element);
                break;
        }

        if (uiElement is null)
        {
            return Result<UIElement>.Fail($"Failed to create element type: {elementTypeNode.GetString()}"); 
        }

        return Result<UIElement>.Ok(uiElement);
    }

    private static StackPanel? CreateStackPanel(JsonElement element)
    {
        var stackPanel = new StackPanel();

        // Set the orientation
        if (element.TryGetProperty("orientation", out var orientation))
        {
            stackPanel.Orientation = orientation.GetString() == "Horizontal" ? Orientation.Horizontal : Orientation.Vertical;
        }

        // Handle children
        if (element.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var createResult = CreateControlFromJson(child);
                if (createResult.IsFailure)
                {
                    return null;
                }
                var childControl = createResult.Value;

                if (childControl != null)
                {
                    stackPanel.Children.Add(childControl);
                }
            }
        }

        return stackPanel;
    }

    private static TextBox CreateTextBox(JsonElement element)
    {
        var textBox = new TextBox();

        // Set binding for the "bindKey" property
        //if (element.TryGetProperty("bindKey", out var bindKey))
        //{
        //    var binding = new Binding
        //    {
        //        Path = new Windows.UI.Xaml.PropertyPath(bindKey.GetString()),
        //        Mode = BindModeTwoWay(element)
        //    };
        //    textBox.SetBinding(TextBox.TextProperty, binding);
        //}

        // Set other properties like placeholder
        if (element.TryGetProperty("placeholder", out var placeholder))
        {
            textBox.PlaceholderText = placeholder.GetString();
        }

        // Set spell checking based on the "checkSpelling" property
        if (element.TryGetProperty("checkSpelling", out var checkSpelling))
        {
            textBox.IsSpellCheckEnabled = checkSpelling.GetBoolean();
        }

        return textBox;
    }

    private static Button CreateButton(JsonElement element)
    {
        var button = new Button();

        // Set the button text
        if (element.TryGetProperty("text", out var text))
        {
            button.Content = text.GetString();
        }

        //// Set the button command (you can expand this to a real command later)
        //if (element.TryGetProperty("command", out var command))
        //{
        //    // For now, just set the Command property to the command string
        //    button.Click += (sender, args) =>
        //    {
        //        // Handle command logic here (this can be expanded based on your needs)
        //        Console.WriteLine($"Button command: {command.GetString()}");
        //    };
        //}

        return button;
    }

    private static BindingMode BindModeTwoWay(JsonElement element)
    {
        // Default to TwoWay binding if not specified
        if (element.TryGetProperty("bindMode", out var bindMode))
        {
            return bindMode.GetString() == "TwoWay" ? BindingMode.TwoWay : BindingMode.OneWay;
        }

        return BindingMode.TwoWay;
    }
}
