using System.Text.Json;
using Celbridge.Forms;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBoxViewModel : ElementViewModel
{
    public static Result<UIElement> CreateTextBox(JsonElement jsonElement, IFormDataProvider formDataProvider)
    {
        // Create the TextBox view model
        var viewModel = ServiceLocator.AcquireService<TextBoxViewModel>();
        viewModel.FormDataProvider = formDataProvider;

        // Create the TextBox view
        var textBox = new TextBox();
        textBox.DataContext = viewModel;

        // Todo: Set this from a property
        textBox.TextWrapping = TextWrapping.Wrap;

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!viewModel.ApplyAlignmentConfig(textBox, jsonElement, buildErrors))
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
        if (!viewModel.ValidateConfigKeys(jsonElement, validConfigKeys, buildErrors))
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

        if (viewModel.GetBindingPropertyPath(jsonElement, "textBinding", out var propertyPath, buildErrors))
        {
            viewModel.ApplyBinding(textBox, TextBox.TextProperty, BindingMode.TwoWay, propertyPath, buildErrors);
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

        var initResult = viewModel.Initialize();
        if (initResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to initialize TextBox view model")
                .WithErrors(initResult);
        }

        return Result<UIElement>.Ok(textBox);
    }
}
