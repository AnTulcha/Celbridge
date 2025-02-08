using System.Text.Json;
using Celbridge.UserInterface.Services.Forms;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class StackPanelViewModel : ElementViewModel
{
    public const int DefaultStackPanelSpacing = 8;

    public static Result<UIElement> CreateStackPanel(JsonElement jsonElement, FormBuilder formBuilder)
    {
        var viewModel = ServiceLocator.AcquireService<StackPanelViewModel>();
        viewModel.FormDataProvider = formBuilder.FormDataProvider;

        return viewModel.InitializeElement(jsonElement, formBuilder);
    }

    private Result<UIElement> InitializeElement(JsonElement jsonElement, FormBuilder formBuilder)
    {
        var stackPanel = new StackPanel();
        stackPanel.DataContext = this;

        // Todo: Use result pattern instead of populating this list
        var buildErrors = new List<string>();

        if (!ApplyAlignmentConfig(stackPanel, jsonElement, buildErrors))
        {
            return Result<UIElement>.Fail($"Failed to apply alignment configuration to StackPanel");
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
                buildErrors.Add($"Invalid orientation value: '{orientationString}'");
            }
        }

        // Add child controls
        if (jsonElement.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
            {
                var childControl = formBuilder.CreateUIElementFromJsonElement(child);
                if (childControl is null)
                {
                    buildErrors.Add("Failed to create child control");
                    continue;
                }

                stackPanel.Children.Add(childControl);
            }
        }

        var initResult = ApplyBindings();
        if (initResult.IsFailure)
        {
            return Result<UIElement>.Fail($"Failed to initialize StackPanel view model");
        }

        return Result<UIElement>.Ok(stackPanel);
    }

}
