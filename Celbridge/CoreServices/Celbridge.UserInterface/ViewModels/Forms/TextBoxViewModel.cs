using System.Text.Json;
using Celbridge.Forms;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBoxViewModel : ElementViewModel
{
    public static Result<UIElement> CreateTextBox(JsonElement jsonElement, IFormDataProvider formDataProvider)
    {
        var viewModel = ServiceLocator.AcquireService<TextBoxViewModel>();
        viewModel.FormDataProvider = formDataProvider;

        return viewModel.InitializeElement(jsonElement);
    }

    private Result<UIElement> InitializeElement(JsonElement jsonElement)
    {
        // Create the TextBox view
        var textBox = new TextBox();
        textBox.DataContext = this;

        // Todo: Set this from a property
        textBox.TextWrapping = TextWrapping.Wrap;

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!ApplyAlignmentConfig(textBox, jsonElement, buildErrors))
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
        if (!ValidateConfigKeys(jsonElement, validConfigKeys, buildErrors))
        {
            return Result<UIElement>.Fail("Invalid TextBox configuration");
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

        if (GetBindingPropertyPath(jsonElement, "textBinding", out var propertyPath, buildErrors))
        {
            ApplyBinding(textBox, TextBox.TextProperty, BindingMode.TwoWay, propertyPath, buildErrors);
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

        var applyResult = ApplyBindings();
        if (applyResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply bindings")
                .WithErrors(applyResult);
        }

        return Result<UIElement>.Ok(textBox);
    }
}
