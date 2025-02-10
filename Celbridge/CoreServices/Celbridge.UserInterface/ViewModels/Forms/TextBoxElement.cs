using System.Text.Json;
using Celbridge.UserInterface.Services.Forms;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBoxElement : FormElement
{
    public static Result<UIElement> CreateTextBox(JsonElement config, FormBuilder formBuilder)
    {
        var formElement = ServiceLocator.AcquireService<TextBoxElement>();
        return formElement.CreateUIElement(config, formBuilder);
    }

    protected override Result<UIElement> CreateUIElement(JsonElement config, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        // Create the TextBox view
        var textBox = new TextBox();
        textBox.DataContext = this;

        // Todo: Set this from a property
        textBox.TextWrapping = TextWrapping.Wrap;

        var alignmentResult = ApplyCommonConfig(textBox, config);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration to TextBox");
        }

        // Check for unsupported config properties

        var validConfigKeys = new HashSet<string>()
        {
            "text",
            "header",
            "placeholder",
            "checkSpelling"
        };

        var validateResult = ValidateConfigKeys(config, validConfigKeys);
        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid TextBox configuration")
                .WithErrors(validateResult);
        }

        // Apply unbound properties

        if (config.TryGetProperty("header", out var header))
        {
            // Todo: Support localization
            textBox.Header = header.GetString();
        }

        if (config.TryGetProperty("placeholder", out var placeholder))
        {
            textBox.PlaceholderText = placeholder.GetString();
        }

        if (config.TryGetProperty("checkSpelling", out var checkSpelling))
        {
            textBox.IsSpellCheckEnabled = checkSpelling.GetBoolean();
        }

        // Apply property bindings

        if (config.TryGetProperty("text", out var textProperty))
        {
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<UIElement>.Fail("'text' property must be a string");
            }
            var text = textProperty.GetString()!;

            if (text.StartsWith('/'))
            {
                var bindingResult = ApplyBinding(textBox, TextBox.TextProperty, BindingMode.TwoWay, text);
                if (bindingResult.IsFailure)
                {
                    return Result<UIElement>.Fail("Failed to apply text binding")
                        .WithErrors(bindingResult);
                }
            }
            else
            {
                return Result<UIElement>.Fail("'text' property must specify a binding");
            }
        }

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

        var finalizeResult = Finalize();
        if (finalizeResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to finalize TextBox element")
                .WithErrors(finalizeResult);
        }

        return Result<UIElement>.Ok(textBox);
    }
}
