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

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!ApplyAlignmentConfig(textBlock, jsonElement, buildErrors))
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration");
        }

        // Check all specified properties are supported

        var validConfigKeys = new HashSet<string>()
        {
            "textBinding",
            "text",
            "italic",
            "bold"
        };

        if (!ValidateConfigKeys(jsonElement, validConfigKeys, buildErrors))
        {
            return Result<UIElement>.Fail("Invalid TextBlock configuration");
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

        if (GetBindingPropertyPath(jsonElement, "textBinding", out var propertyPath, buildErrors))
        {
            ApplyBinding(textBlock, TextBlock.TextProperty, BindingMode.OneWay, propertyPath, buildErrors);
        }

        if (buildErrors.Count != 0)
        {
            var buildError = buildErrors[0];
            return Result<UIElement>.Fail(buildError);
        }

        var applyResult = ApplyBindings();
        if (applyResult.IsFailure)
        {
            return Result<UIElement>.Fail("Failed to apply bindings")
                .WithErrors(applyResult);
        }

        return Result<UIElement>.Ok(textBlock);
    }
}
