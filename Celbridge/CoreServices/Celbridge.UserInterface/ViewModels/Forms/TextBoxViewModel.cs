using System.Text.Json;
using Celbridge.UserInterface.Services.Forms;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBoxViewModel : ElementViewModel
{
    public static Result<UIElement> CreateTextBox(JsonElement jsonElement, FormBuilder formBuilder)
    {
        var viewModel = ServiceLocator.AcquireService<TextBoxViewModel>();
        return viewModel.CreateElement(jsonElement, formBuilder);
    }

    protected override Result<UIElement> CreateElement(JsonElement jsonElement, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        // Create the TextBox view
        var textBox = new TextBox();
        textBox.DataContext = this;

        // Todo: Set this from a property
        textBox.TextWrapping = TextWrapping.Wrap;

        var alignmentResult = ApplyAlignmentConfig(textBox, jsonElement);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration to TextBox");
        }

        // Check for unsupported config properties

        var validConfigKeys = new HashSet<string>()
        {
            "textBinding",
            "header",
            "placeholder",
            "checkSpelling"
        };

        var validateResult = ValidateConfigKeys(jsonElement, validConfigKeys);
        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid TextBox configuration")
                .WithErrors(validateResult);
        }

        // Apply unbound properties

        if (jsonElement.TryGetProperty("header", out var header))
        {
            // Todo: Support localization
            textBox.Header = header.GetString();
        }

        if (jsonElement.TryGetProperty("placeholder", out var placeholder))
        {
            textBox.PlaceholderText = placeholder.GetString();
        }

        if (jsonElement.TryGetProperty("checkSpelling", out var checkSpelling))
        {
            textBox.IsSpellCheckEnabled = checkSpelling.GetBoolean();
        }

        // Apply property bindings

        var pathResult = GetBindingPropertyPath(jsonElement, "textBinding");
        if (pathResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to get text binding property path")
                .WithErrors(pathResult);
        }
        var (hasBinding, propertyPath) = pathResult.Value;

        if (hasBinding)
        {
            var bindingResult = ApplyBinding(textBox, TextBox.TextProperty, BindingMode.TwoWay, propertyPath);
            if (bindingResult.IsFailure)
            {
                return Result<UIElement>.Fail($"Failed to apply text binding")
                    .WithErrors(bindingResult);
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
