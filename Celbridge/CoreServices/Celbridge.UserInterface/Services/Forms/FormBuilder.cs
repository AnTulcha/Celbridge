using Celbridge.Forms;
using Celbridge.Logging;
using Celbridge.UserInterface.ViewModels.Forms;
using System.Text.Json;

namespace Celbridge.UserInterface.Services.Forms;

public class FormBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FormBuilder> _logger;

    private IFormDataProvider? _formDataProvider;
    public IFormDataProvider FormDataProvider => _formDataProvider!;

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
            Spacing = StackPanelViewModel.DefaultStackPanelSpacing
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

    public UIElement? CreateUIElementFromJsonElement(JsonElement jsonElement)
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
                var stackPanelResult = StackPanelViewModel.CreateStackPanel(jsonElement, this);
                if (stackPanelResult.IsFailure)
                {
                    _buildErrors.Add(stackPanelResult.Error);
                }
                else
                {
                    uiElement = stackPanelResult.Value;
                }
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
                var buttonResult = ButtonViewModel.CreateButton(jsonElement, _formDataProvider);
                if (buttonResult.IsFailure)
                {
                    _buildErrors.Add(buttonResult.Error);
                }
                else
                {
                    uiElement = buttonResult.Value;
                }
                break;
        }

        if (uiElement is null)
        {
            _buildErrors.Add($"Failed to create element of type: '{elementName}'");
            return null;
        }

        return uiElement;
    }
}
