using Celbridge.UserInterface.Services.Forms;
using Microsoft.UI.Text;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.UI.Text;

namespace Celbridge.UserInterface.ViewModels.Forms;

public partial class TextBlockElement : FormElement
{
    public static Result<UIElement> CreateTextBlock(JsonElement config, FormBuilder formBuilder)
    {
        var formElement = ServiceLocator.AcquireService<TextBlockElement>();
        return formElement.CreateUIElement(config, formBuilder);
    }

    [ObservableProperty]
    private string _text = string.Empty;
    private string _textPropertyPath = string.Empty;

    protected override Result<UIElement> CreateUIElement(JsonElement config, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        //
        // Create the UI element and set the data context
        //

        var textBlock = new TextBlock();
        textBlock.DataContext = this;
        bool hasBindings = false;

        //
        // Check all config properties are supported
        //

        var validateResult = ValidateConfigKeys(config, new HashSet<string>()
        {
            "text",
            "italic",
            "bold"
        });

        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid form element configuration")
                .WithErrors(validateResult);
        }

        //
        // Apply common element config properties
        //

        var alignmentResult = ApplyCommonConfig(textBlock, config);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply common config properties")
                .WithErrors(alignmentResult);
        }

        //
        // Apply element-specific config properties
        //

        var italicResult = ApplyItalicConfig(config, textBlock);
        if (italicResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'italic' config property")
                .WithErrors(italicResult);
        }

        var boldResult = ApplyBoldConfig(config, textBlock);
        if (boldResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'bold' config property")
                .WithErrors(boldResult);
        }

        var textResult = ApplyTextConfig(config, textBlock);
        if (textResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'text' config property")
                .WithErrors(textResult);
        }
        hasBindings = hasBindings || textResult.Value;

        //
        // Register listeners for bound properties
        //

        if (hasBindings)
        {
            // Listen for changes to update bound values
            FormDataProvider.FormPropertyChanged += OnFormDataChanged;
            PropertyChanged += OnMemberChanged;
            textBlock.Unloaded += OnUIElementUnloaded;
        }

        return Result<UIElement>.Ok(textBlock);
    }

    private Result ApplyItalicConfig(JsonElement config, TextBlock textBlock)
    {
        if (config.TryGetProperty("italic", out var italicValue))
        {
            // Check the type
            if (italicValue.ValueKind != JsonValueKind.True &&
                italicValue.ValueKind != JsonValueKind.False)
            {
                return Result.Fail("'italic' property must be a boolean");
            }

            // Apply the property
            var italic = italicValue.GetBoolean();
            if (italic)
            {
                textBlock.FontStyle = FontStyle.Italic;
            }
        }

        return Result.Ok();
    }

    private Result ApplyBoldConfig(JsonElement config, TextBlock textBlock)
    {
        if (config.TryGetProperty("bold", out var boldValue))
        {
            // Check the type
            if (boldValue.ValueKind != JsonValueKind.True &&
                boldValue.ValueKind != JsonValueKind.False)
            {
                return Result.Fail("'bold' property must be a boolean");
            }

            // Apply the property
            var bold = boldValue.GetBoolean();
            if (boldValue.GetBoolean())
            {
                textBlock.FontWeight = FontWeights.Bold;
            }
        }

        return Result.Ok();
    }

    private Result<bool> ApplyTextConfig(JsonElement config, TextBlock textBlock)
    {
        if (config.TryGetProperty("text", out var textProperty))
        {
            // Check the type
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<bool>.Fail("'text' property must be a string");
            }

            // Apply the property
            var text = textProperty.GetString()!;
            if (text.StartsWith('/'))
            {
                // Store the property path for future updates
                _textPropertyPath = text;

                // Bind dependency property to a member variable on this class
                textBlock.SetBinding(TextBlock.TextProperty, new Binding()
                {
                    Path = new PropertyPath(nameof(Text)),
                    Mode = BindingMode.OneWay
                });

                // Get the current property value via the FormDataProvider
                var updateResult = UpdateText();
                if (updateResult.IsFailure)
                {
                    return Result<bool>.Fail($"Failed to update value of 'text' property")
                        .WithErrors(updateResult);
                }

                return Result<bool>.Ok(true);
            }
            else
            {
                // Set property value directly
                // Todo: Support localization
                textBlock.Text = text;
            }
        }

        return Result<bool>.Ok(false);
    }

    private Result UpdateText()
    {
        Guard.IsNotNull(FormDataProvider);

        // Get the property JSON value from the FormDataProvider
        var getResult = FormDataProvider.GetProperty(_textPropertyPath);
        if (getResult.IsFailure)
        {
            return Result.Fail($"Failed to get property: '{_textPropertyPath}'")
                .WithErrors(getResult);
        }
        var jsonValue = getResult.Value;

        // Parse the JSON value
        var jsonNode = JsonNode.Parse(jsonValue);
        if (jsonNode is null)
        {
            return Result.Fail($"Failed to parse JSON value for property: '{_textPropertyPath}'");
        }
        var value = jsonNode.ToString();

        // Update the member variable
        Text = value;

        return Result.Ok();
    }

    private void OnFormDataChanged(string propertyPath)
    {
        if (propertyPath == _textPropertyPath)
        {
            UpdateText();
        }
    }

    private void OnMemberChanged(object? sender, PropertyChangedEventArgs e)
    {
        Guard.IsNotNull(FormDataProvider);

        // Stop listening for form data changes while we update the form data
        FormDataProvider.FormPropertyChanged -= OnFormDataChanged;

        if (e.PropertyName == nameof(Text))
        {
            var jsonValue = JsonSerializer.Serialize(Text);
            FormDataProvider.SetProperty(_textPropertyPath, jsonValue, false);
        }

        // Resume listening for form data changes
        FormDataProvider.FormPropertyChanged += OnFormDataChanged;
    }
    private void OnUIElementUnloaded(object sender, RoutedEventArgs e)
    {
        // Unregister all listeners and clear references

        Guard.IsNotNull(FormDataProvider);

        FormDataProvider.FormPropertyChanged -= OnFormDataChanged;
        PropertyChanged -= OnMemberChanged;

        var uiElement = sender as FrameworkElement;
        Guard.IsNotNull(uiElement);
        uiElement.Unloaded -= OnUIElementUnloaded;

        FormDataProvider = null;
    }
}
