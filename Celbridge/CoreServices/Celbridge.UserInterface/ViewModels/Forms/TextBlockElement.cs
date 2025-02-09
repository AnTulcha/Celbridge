using Celbridge.UserInterface.Services.Forms;
using Microsoft.UI.Text;
using System.Text.Json;
using Windows.UI.Text;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class TextBlockElement : FormElement
{
    public static Result<UIElement> CreateTextBlock(JsonElement config, FormBuilder formBuilder)
    {
        // Create the TextBlock view model
        var formElement = ServiceLocator.AcquireService<TextBlockElement>();
        return formElement.CreateElement(config, formBuilder);
    }

    protected override Result<UIElement> CreateElement(JsonElement config, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        // Create the TextBlock view
        var textBlock = new TextBlock();
        textBlock.DataContext = this;

        var alignmentResult = ApplyAlignmentConfig(textBlock, config);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration")
                .WithErrors(alignmentResult);
        }

        // Check all specified properties are supported

        var validConfigKeys = new HashSet<string>()
        {
            "text",
            "italic",
            "bold"
        };

        var validateResult = ValidateConfigKeys(config, validConfigKeys);
        if (validateResult.IsFailure)
        {
            return Result<UIElement>.Fail("Invalid TextBlock configuration")
                .WithErrors(validateResult);
        }

        // Apply property bindings

        if (config.TryGetProperty("italic", out var italic))
        {
            if (italic.GetBoolean())
            {
                textBlock.FontStyle = FontStyle.Italic;
            }
        }

        if (config.TryGetProperty("bold", out var bold))
        {
            if (bold.GetBoolean())
            {
                textBlock.FontWeight = FontWeights.Bold;
            }
        }

        // Apply bound properties

        if (config.TryGetProperty("text", out var textProperty))
        {
            if (textProperty.ValueKind != JsonValueKind.String)
            {
                return Result<UIElement>.Fail("'text' property must be a string");
            }
            var text = textProperty.GetString()!;

            if (text.StartsWith('/'))
            {
                var bindingResult = ApplyBinding(textBlock, TextBlock.TextProperty, BindingMode.OneWay, text);
                if (bindingResult.IsFailure)
                {
                    return Result<UIElement>.Fail("Failed to apply text binding")
                        .WithErrors(bindingResult);
                }
            }
            else
            {
                // Todo: Support localization
                textBlock.Text = text;
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
