using Celbridge.Forms;
using Celbridge.Logging;
using Celbridge.UserInterface.ViewModels.Forms;
using Microsoft.UI.Text;
using System.Text.Json;
using Windows.System;
using Windows.UI.Text;

namespace Celbridge.UserInterface.Services.Forms;

public class FormBuilder
{
    private const int DefaultStackPanelSpacing = 8;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FormBuilder> _logger;

    private IFormDataProvider? _formDataProvider;

    private List<string> _buildErrors = new();

    public FormBuilder(
        IServiceProvider serviceProvider,
        ILogger<FormBuilder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Result<object> BuildForm(string formName, JsonElement formConfig, IFormDataProvider formDataProvider)
    {
        _formDataProvider = formDataProvider;
        _buildErrors.Clear();

        var formPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            DataContext = formDataProvider,
            Spacing = DefaultStackPanelSpacing
        };

        try
        {
            foreach (var jsonElement in formConfig.EnumerateArray())
            {
                var uiElement = CreateUIElementFromJsonElement(jsonElement);
                if (uiElement is null)
                {
                    // The build has failed, but continue to report all the errors we can find
                    continue;
                }

                formPanel.Children.Add(uiElement);
            }

        }
        catch (Exception ex)
        {
            _buildErrors.Add($"An exception occurred when building form: {formName}. {ex}");
        }

        // Build has completed.
        // If any errors were encountered then fail the build, otherwise return the new root panel.

        if (_buildErrors.Count > 0)
        {
            // Log all build errors encountered (in reverse order)
            _logger.LogError($"Failed to build form: '{formName}'");
            _buildErrors.Reverse();
            foreach (var error in _buildErrors)
            {
                _logger.LogError(error);
            }
            _buildErrors.Clear();
            _formDataProvider = null;

            // Fail the build
            return Result<object>.Fail($"Failed to build form: {formName}");
        }
        _formDataProvider = null;

        formPanel.Loaded += (s, e) =>
        {
            var formDataProvider = formPanel.DataContext as IFormDataProvider;
            if (formDataProvider is not null)
            {
                formDataProvider.OnFormLoaded();
            }
        };

        formPanel.Unloaded += (s, e) =>
        {
            var formDataProvider = formPanel.DataContext as IFormDataProvider;
            if (formDataProvider is not null)
            {
                formDataProvider.OnFormUnloaded();
            }
        };

        return Result<object>.Ok(formPanel);
    }

    private UIElement? CreateUIElementFromJsonElement(JsonElement jsonElement)
    {
        Guard.IsNotNull(_formDataProvider);

        if (jsonElement.ValueKind != JsonValueKind.Object)
        {
            _buildErrors.Add("Form array element is not an object");
            return null;
        }

        if (!jsonElement.TryGetProperty("element", out var element))
        {
            _buildErrors.Add("Form object does not contain an 'elementType' property");
            return null;
        }
        var elementName = element.GetString();

        UIElement? uiElement = null;
        switch (elementName)
        {
            case "StackPanel":
                uiElement = CreateStackPanel(jsonElement);
                break;

            case "TextBox":
                var textBoxResult = TextBoxViewModel.CreateTextBox(jsonElement, _formDataProvider);
                if (textBoxResult.IsFailure)
                {
                    _buildErrors.Add(textBoxResult.Error);
                }
                else
                {
                    uiElement = textBoxResult.Value;
                }
                break;

            case "TextBlock":
                var textBlockResult = TextBlockViewModel.CreateTextBlock(jsonElement, _formDataProvider);
                if (textBlockResult.IsFailure)
                {
                    _buildErrors.Add(textBlockResult.Error);
                }
                else
                {
                    uiElement = textBlockResult.Value;
                }
                break;

            case "Button":
                uiElement = CreateButton(jsonElement);
                break;
        }

        if (uiElement is null)
        {
            _buildErrors.Add($"Failed to create element of type: '{elementName}'");
            return null;
        }

        return uiElement;
    }

    private StackPanel? CreateStackPanel(JsonElement jsonElement)
    {
        Guard.IsNotNull(_formDataProvider);

        var stackPanel = new StackPanel();

        var viewModel = _serviceProvider.GetRequiredService<StackPanelViewModel>();
        viewModel.FormDataProvider = _formDataProvider;

        stackPanel.DataContext = viewModel;

        if (!viewModel.ApplyAlignmentConfig(stackPanel, jsonElement, _buildErrors))
        {
            _buildErrors.Add($"Failed to apply alignment configuration to StackPanel");
            return null;
        }

        // Set the spacing between elements
        if (jsonElement.TryGetProperty("spacing", out var spacing))
        {
            stackPanel.Spacing = spacing.GetInt32();
        }
        else
        {
            stackPanel.Spacing = DefaultStackPanelSpacing;
        }

        // Set the orientation
        if (jsonElement.TryGetProperty("orientation", out var orientation))
        {
            var orientationString = orientation.GetString();
            if (orientationString == "Horizontal")
            {
                stackPanel.Orientation = Orientation.Horizontal;
            }
            else if (orientationString == "Vertical")
            {
                stackPanel.Orientation = Orientation.Vertical;
            }
            else
            {
                // Log the error and default to vertical
                _buildErrors.Add($"Invalid orientation value: '{orientationString}'");
            }
        }

        // Add child controls
        if (jsonElement.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var childControl = CreateUIElementFromJsonElement(child);
                if (childControl is null)
                {
                    _buildErrors.Add("Failed to create child control");
                    continue;
                }

                stackPanel.Children.Add(childControl);
            }
        }

        viewModel.Initialize();

        return stackPanel;
    }

    private Button? CreateButton(JsonElement jsonElement)
    {
        Guard.IsNotNull(_formDataProvider);

        var button = new Button();

        var viewModel = _serviceProvider.GetRequiredService<ButtonViewModel>();
        viewModel.FormDataProvider = _formDataProvider;

        button.DataContext = viewModel;

        if (!viewModel.ApplyAlignmentConfig(button, jsonElement, _buildErrors))
        {
            _buildErrors.Add($"Failed to apply alignment configuration to Button");
            return null;
        }

        viewModel.ApplyTooltip(button, jsonElement);

        // Check all specified properties are supported

        var validConfigKeys = new HashSet<string>()
        {
            "icon",
            "text",
            "enabledBinding",
            "buttonId"
        };
        if (!viewModel.ValidateConfigKeys(jsonElement, validConfigKeys, _buildErrors))
        {
            _buildErrors.Add("Invalid Button configuration");
            return null;
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

        viewModel.ButtonId = buttonId;

        button.DataContext = viewModel;

        if (!string.IsNullOrEmpty(buttonId))
        {
            // Bind the button click handler to the button view model
            button.Click += (sender, args) =>
            {
                viewModel.OnButtonClicked();
            };
        }

        viewModel.Initialize();

        return button;
    }
}
