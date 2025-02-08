using System.Text.Json;
using Celbridge.Forms;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class ButtonViewModel : ElementViewModel, IButtonViewModel
{
    public string ButtonId = string.Empty;

    public void OnButtonClicked()
    {
        FormDataProvider?.OnButtonClicked(ButtonId);
    }

    public static Result<UIElement> CreateButton(JsonElement jsonElement, IFormDataProvider formDataProvider)
    {
        var viewModel = ServiceLocator.AcquireService<ButtonViewModel>();
        viewModel.FormDataProvider = formDataProvider;

        return viewModel.InitializeElement(jsonElement);
    }

    private Result<UIElement> InitializeElement(JsonElement jsonElement)
    {
        var button = new Button();
        button.DataContext = this;

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!ApplyAlignmentConfig(button, jsonElement, buildErrors))
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration to Button");
        }

        ApplyTooltip(button, jsonElement);

        // Check all specified properties are supported

        var validConfigKeys = new HashSet<string>()
        {
            "icon",
            "text",
            "enabledBinding",
            "buttonId"
        };
        if (!ValidateConfigKeys(jsonElement, validConfigKeys, buildErrors))
        {
            return Result<UIElement>.Fail("Invalid Button configuration");
        }

        // Add a horizontal panel for the button content

        var buttonPanel = new StackPanel();
        buttonPanel.Orientation = Orientation.Horizontal;
        button.Content = buttonPanel;

        //
        // Set the button icon (optional)
        //

        var buttonIcon = string.Empty;
        if (jsonElement.TryGetProperty("icon", out var icon))
        {
            buttonIcon = icon.GetString();
        }

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

        //
        // Set the button text (optional)
        //

        var buttonText = string.Empty;
        if (jsonElement.TryGetProperty("text", out var text))
        {
            buttonText = text.GetString();
        }

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

        // Get the buttonId
        string buttonId = string.Empty;
        if (jsonElement.TryGetProperty("buttonId", out var buttonIdElement))
        {
            buttonId = buttonIdElement.GetString() ?? string.Empty;
        }

        ButtonId = buttonId;

        if (!string.IsNullOrEmpty(buttonId))
        {
            // Bind the button click handler to the button view model
            button.Click += (sender, args) =>
            {
                OnButtonClicked();
            };
        }

        var initResult = ApplyBindings();
        if (initResult.IsFailure)
        {
            return Result<UIElement>.Fail("Failed to initialize Button view model")
                .WithErrors(initResult);
        }

        return Result<UIElement>.Ok(button);
    }
}
