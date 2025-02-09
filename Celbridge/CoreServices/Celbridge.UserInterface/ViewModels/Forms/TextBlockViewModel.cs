using Celbridge.Forms;
using Microsoft.UI.Text;
using System.Text.Json;
using Windows.UI.Text;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBlockViewModel : ElementViewModel
{
    public static Result<UIElement> CreateTextBlock(JsonElement jsonElement, IFormDataProvider formDataProvider)
    {
        // Create the TextBlock view model
        var viewModel = ServiceLocator.AcquireService<TextBlockViewModel>();
        viewModel.FormDataProvider = formDataProvider;

        return viewModel.InitializeElement(jsonElement);
    }

    private Result<UIElement> InitializeElement(JsonElement jsonElement)
    {
        // Create the TextBlock view
        var textBlock = new TextBlock();
        textBlock.DataContext = this;

        var alignmentResult = ApplyAlignmentConfig(textBlock, jsonElement);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration")
                .WithErrors(alignmentResult);
        }

        // Check all specified properties are supported

        var validConfigKeys = new HashSet<string>()
        {
            "textBinding",
            "text",
            "italic",
            "bold"
        };

        var validateResult = ValidateConfigKeys(jsonElement, validConfigKeys);
        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid TextBlock configuration")
                .WithErrors(validateResult);
        }

        // Apply property bindings

        if (jsonElement.TryGetProperty("text", out var text))
        {
            // Todo: Support localization
            textBlock.Text = text.GetString();
        }

        if (jsonElement.TryGetProperty("italic", out var italic))
        {
            if (italic.GetBoolean())
            {
                textBlock.FontStyle = FontStyle.Italic;
            }
        }

        if (jsonElement.TryGetProperty("bold", out var bold))
        {
            if (bold.GetBoolean())
            {
                textBlock.FontWeight = FontWeights.Bold;
            }
        }

        // Apply bound properties

        var pathResult = GetBindingPropertyPath(jsonElement, "textBinding");
        if (pathResult.IsFailure)
        {
            return Result<UIElement>.Fail("Failed to get binding property path")
                .WithErrors(pathResult);
        }
        var (hasBinding, propertyPath) = pathResult.Value;

        if (hasBinding)
        {
            var bindingResult = ApplyBinding(textBlock, TextBlock.TextProperty, BindingMode.OneWay, propertyPath);
            if (bindingResult.IsFailure)
            {
                return Result<UIElement>.Fail("Failed to apply text binding")
                    .WithErrors(bindingResult);
            }
        }

        var finalizeResult = Finalize();
        if (finalizeResult.IsFailure)
        {
            return Result<UIElement>.Fail("Failed to finalize TextBlock element")
                .WithErrors(finalizeResult);
        }

        return Result<UIElement>.Ok(textBlock);
    }
}
