using Celbridge.Forms;
using Celbridge.UserInterface.Services.Forms;
using System.Text.Json;

namespace Celbridge.UserInterface.ViewModels.Forms;

public partial class ButtonElement : FormElement
{
    public static Result<UIElement> CreateButton(JsonElement config, FormBuilder formBuilder)
    {
        var formElement = ServiceLocator.AcquireService<ButtonElement>();
        return formElement.CreateUIElement(config, formBuilder);
    }

    protected override Result<UIElement> CreateUIElement(JsonElement config, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        //
        // Create the UI element
        //

        var button = new Button();
        button.DataContext = this;

        // Add a horizontal panel for the button content
        var buttonPanel = new StackPanel();
        buttonPanel.Orientation = Orientation.Horizontal;
        button.Content = buttonPanel;

        //
        // Check all specified config properties are supported
        //

        var validateResult = ValidateConfigKeys(config, new HashSet<string>()
        {
            "icon",
            "text",
            "buttonId"
        });

        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid Button configuration")
                .WithErrors(validateResult);
        }

        //
        // Apply common config properties
        //

        var commonConfigResult = ApplyCommonConfig(button, config);
        if (commonConfigResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply common config properties")
                .WithErrors(commonConfigResult);
        }

        //
        // Apply element-specific config properties
        //

        var iconResult = ApplyIconConfig(config, buttonPanel);
        if (iconResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'icon' config property")
                .WithErrors(iconResult);
        }

        var textResult = ApplyTextConfig(config, buttonPanel);
        if (textResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'text' config property")
                .WithErrors(textResult);
        }

        var buttonIdResult = ApplyButtonIdConfig(config, button);
        if (buttonIdResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'buttonId' config property")
                .WithErrors(buttonIdResult);
        }

        return Result<UIElement>.Ok(button);
    }

    private Result ApplyIconConfig(JsonElement config, StackPanel buttonPanel)
    {
        if (config.TryGetProperty("icon", out var textProperty))
        {
            // Check the type
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<bool>.Fail("'icon' property must be a string");
            }

            // Apply the property
            var buttonIcon = textProperty.GetString();

            if (!string.IsNullOrEmpty(buttonIcon))
            {
                string glyph = string.Empty;
                if (Enum.TryParse(buttonIcon, out Symbol symbol))
                {
                    // String is a valid Symbol enum value
                    glyph = ((char)symbol).ToString();
                }
                else
                {
                    // Try the string as a unicode character
                    glyph = buttonIcon;
                }

                var fontIcon = new FontIcon()
                    .Glyph(glyph);

                buttonPanel.Children.Add(fontIcon);
            }
        }

        return Result.Ok();
    }

    private Result ApplyTextConfig(JsonElement config, StackPanel buttonPanel)
    {
        if (config.TryGetProperty("text", out var textProperty))
        {
            // Check the type
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<bool>.Fail("'text' property must be a string");
            }

            // Apply the property
            var buttonText = textProperty.GetString();

            if (!string.IsNullOrEmpty(buttonText))
            {
                var textBlock = new TextBlock()
                    .Text(buttonText);

                if (buttonPanel.Children.Count > 0)
                {
                    // Add a gap between the icon and the text
                    textBlock.Margin = new Thickness(8, 0, 0, 0);
                }

                buttonPanel.Children.Add(textBlock);
            }
        }

        return Result.Ok();
    }

    private Result ApplyButtonIdConfig(JsonElement config, Button button)
    {
        if (config.TryGetProperty("buttonId", out var textProperty))
        {
            // Check the type
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<bool>.Fail("'text' property must be a string");
            }

            // Apply the property
            var buttonId = textProperty.GetString();

            if (string.IsNullOrEmpty(buttonId))
            {
                return Result<bool>.Fail("'buttonId' property must not be empty");
            }

            button.Click += (sender, args) =>
            {
                // Handle the button click
                OnButtonClicked(buttonId);
            };
        }

        return Result.Ok();
    }

    private void OnButtonClicked(string buttonId)
    {
        FormDataProvider?.OnButtonClicked(buttonId);
    }
}
