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

        // Create the TextBlock view
        var textBlock = new TextBlock();
        textBlock.DataContext = viewModel;

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!viewModel.ApplyAlignmentConfig(textBlock, jsonElement, buildErrors))
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

        if (!viewModel.ValidateConfigKeys(jsonElement, validConfigKeys, buildErrors))
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

        if (viewModel.GetBindingPropertyPath(jsonElement, "textBinding", out var propertyPath, buildErrors))
        {
            viewModel.ApplyBinding(textBlock, TextBlock.TextProperty, BindingMode.OneWay, propertyPath, buildErrors);
        }

        if (buildErrors.Count != 0)
        {
            var buildError = buildErrors[0];
            return Result<UIElement>.Fail(buildError);
        }

        var initResult = viewModel.Initialize();
        if (initResult.IsFailure)
        {
            return Result<UIElement>.Fail("Failed to initialize TextBlock view model")
                .WithErrors(initResult);
        }

        return Result<UIElement>.Ok(textBlock);
    }
}
