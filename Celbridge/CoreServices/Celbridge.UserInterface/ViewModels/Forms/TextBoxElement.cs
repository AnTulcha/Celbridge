using Celbridge.UserInterface.Services.Forms;
using System.Text.Json.Nodes;
using System.Text.Json;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels.Forms;

public partial class TextBoxElement : FormElement
{
    public static Result<UIElement> CreateTextBox(JsonElement config, FormBuilder formBuilder)
    {
        var formElement = ServiceLocator.AcquireService<TextBoxElement>();
        return formElement.CreateUIElement(config, formBuilder);
    }

    [ObservableProperty]
    private string _text = string.Empty;
    private string _textPropertyPath = string.Empty;

    protected override Result<UIElement> CreateUIElement(JsonElement config, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        //
        // Create the UI element
        //

        var textBox = new TextBox();
        textBox.DataContext = this;
        bool hasBindings = false;

        textBox.KeyDown += (sender, e) =>
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Pressing enter moves focus to next focusable element
                var options = new FindNextElementOptions
                {
                    SearchRoot = ((UIElement)sender).XamlRoot!.Content
                };

                FocusManager.TryMoveFocus(FocusNavigationDirection.Next, options);

                e.Handled = true;
            }
        };

        //
        // Check all specified config properties are supported
        //

        var validateResult = ValidateConfigKeys(config, new HashSet<string>()
        {
            "text",
            "header",
            "placeholder",
            "checkSpelling"
        });

        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid form element configuration")
                .WithErrors(validateResult);
        }

        //
        // Apply common element config properties
        //

        var commonConfigResult = ApplyCommonConfig(textBox, config);
        if (commonConfigResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply common config properties")
                .WithErrors(commonConfigResult);
        }

        //
        // Apply element-specific config properties
        //

        // Todo: Set this via a config property
        textBox.TextWrapping = TextWrapping.Wrap;

        var headerResult = ApplyHeaderConfig(config, textBox);
        if (headerResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'header' config property")
                .WithErrors(headerResult);        
        }

        var placeholderResult = ApplyPlaceholderConfig(config, textBox);
        if (placeholderResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'placeholder' config property")
                .WithErrors(placeholderResult);
        }

        var checkSpellingResult = ApplyCheckSpellingConfig(config, textBox);
        if (checkSpellingResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'checkSpelling' config property")
                .WithErrors(checkSpellingResult);
        }

        var textResult = ApplyTextConfig(config, textBox);
        if (textResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply 'text' config property")
                .WithErrors(textResult);
        }
        hasBindings = hasBindings || textResult.Value;

        //
        // Finalize bindings
        //

        if (hasBindings)
        {
            Bind(textBox);
        }

        return Result<UIElement>.Ok(textBox);
    }

    private Result ApplyHeaderConfig(JsonElement config, TextBox textBox)
    {
        if (config.TryGetProperty("header", out var jsonValue))
        {
            // Check the type
            if (jsonValue.ValueKind != JsonValueKind.String)
            {
                return Result.Fail("'header' property must be a string");
            }

            // Todo: Support binding

            // Apply the property
            var header = jsonValue.GetString();
            textBox.Header = header;
        }

        return Result.Ok();
    }

    private Result ApplyPlaceholderConfig(JsonElement config, TextBox textBox)
    {
        if (config.TryGetProperty("placeholder", out var jsonValue))
        {
            // Check the type
            if (jsonValue.ValueKind != JsonValueKind.String)
            {
                return Result.Fail("'placeholder' property must be a string");
            }

            // Todo: Support binding

            // Apply the property
            var placeholder = jsonValue.GetString();
            textBox.PlaceholderText = placeholder;
        }

        return Result.Ok();
    }

    private Result ApplyCheckSpellingConfig(JsonElement config, TextBox textBox)
    {
        if (config.TryGetProperty("checkSpelling", out var jsonValue))
        {
            // Check the type
            if (jsonValue.ValueKind != JsonValueKind.True &&
                jsonValue.ValueKind != JsonValueKind.False)
            {
                return Result.Fail("'checkSpelling' property must be a boolean");
            }

            // Apply the property
            var checkSpelling = jsonValue.GetBoolean();
            textBox.IsSpellCheckEnabled = checkSpelling;
        }

        return Result.Ok();
    }

    private Result<bool> ApplyTextConfig(JsonElement config, TextBox textBox)
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
                textBox.SetBinding(TextBox.TextProperty, new Binding()
                {
                    Path = new PropertyPath(nameof(Text)),
                    Mode = BindingMode.TwoWay
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
                return Result<bool>.Fail($"'text' property must specify a form property binding");
            }
        }

        return Result<bool>.Ok(false);
    }

    private Result UpdateText()
    {
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

    protected override void OnFormDataChanged(string propertyPath)
    {
        if (propertyPath == _textPropertyPath)
        {
            UpdateText();
        }
    }

    protected override void OnMemberDataChanged(string propertyName)
    {
        if (propertyName == nameof(Text))
        {
            var jsonValue = JsonSerializer.Serialize(Text);
            FormDataProvider.SetProperty(_textPropertyPath, jsonValue, false);
        }
    }
}
