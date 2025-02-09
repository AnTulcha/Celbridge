using System.Text.Json;
using Celbridge.UserInterface.Services.Forms;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class StackPanelViewModel : ElementViewModel
{
    public const int DefaultStackPanelSpacing = 8;

    public static Result<UIElement> CreateStackPanel(JsonElement jsonElement, FormBuilder formBuilder)
    {
        var viewModel = ServiceLocator.AcquireService<StackPanelViewModel>();
        return viewModel.CreateElement(jsonElement, formBuilder);
    }

    protected override Result<UIElement> CreateElement(JsonElement jsonElement, FormBuilder formBuilder)
    {
        FormDataProvider = formBuilder.FormDataProvider;

        var stackPanel = new StackPanel();
        stackPanel.DataContext = this;

        var alignmentResult = ApplyAlignmentConfig(stackPanel, jsonElement);
        if (alignmentResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration to StackPanel")
                .WithErrors(alignmentResult);
        }

        // Set the spacing between elements
        if (jsonElement.TryGetProperty("spacing", out var spacing))
        {
            stackPanel.Spacing = spacing.GetInt32();
        }
        else
        {
            stackPanel.Spacing = DefaultStackPanelSpacing;
        }

        // Set the orientation
        if (jsonElement.TryGetProperty("orientation", out var orientation))
        {
            var orientationString = orientation.GetString();
            if (orientationString == "Horizontal")
            {
                stackPanel.Orientation = Orientation.Horizontal;
            }
            else if (orientationString == "Vertical")
            {
                stackPanel.Orientation = Orientation.Vertical;
            }
            else
            {
                // Log the error and default to vertical
                return Result<UIElement>.Fail($"Invalid orientation value: '{orientationString}'");
            }
        }

        // Add child controls
        if (jsonElement.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var childControl = formBuilder.CreateFormElement(child);
                if (childControl is null)
                {
                    return Result<UIElement>.Fail("Failed to create child control");
                }

                stackPanel.Children.Add(childControl);
            }
        }

        var finalizeResult = Finalize();
        if (finalizeResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to finalize StackPanel element");
        }

        return Result<UIElement>.Ok(stackPanel);
    }

}
