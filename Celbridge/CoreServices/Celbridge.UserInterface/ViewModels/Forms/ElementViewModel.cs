using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Celbridge.Forms;
using Celbridge.UserInterface.Services.Forms;

namespace Celbridge.UserInterface.ViewModels.Forms;

/// <summary>
/// Base class for element view models that bind to a form data provider.
/// </summary>
public abstract partial class ElementViewModel : ObservableObject
{
    [ObservableProperty]
    private string _value = string.Empty;

    public string BoundPropertyName => nameof(Value);

    public IFormDataProvider? FormDataProvider { get; set; }

    public string PropertyPath { get; set; } = string.Empty;

    protected abstract Result<UIElement> CreateElement(JsonElement jsonElement, FormBuilder formBuilder);

    public Result Finalize()
    {
        if (FormDataProvider is null)
        {
            return Result.Fail("Form data provider is null");
        }

        if (!string.IsNullOrEmpty(PropertyPath))
        {
            // Read the current property value from the component
            var updateResult = UpdateValue();
            if (updateResult.IsFailure)
            {
                return Result.Fail($"Failed to update value for element view model")
                    .WithErrors(updateResult);
            }
        }

        // Listen for property changes on the component (via the form data provider)
        FormDataProvider.FormPropertyChanged += OnFormDataPropertyChanged;

        // Listen for property changes on this view model (via ObservableObject)
        PropertyChanged += OnMemberPropertyChanged;

        return Result.Ok();
    }

    public void OnViewUnloaded()
    {
        if (FormDataProvider != null)
        {
            // Unregister listeners
            FormDataProvider.FormPropertyChanged -= OnFormDataPropertyChanged;
            PropertyChanged -= OnMemberPropertyChanged;
        }
    }

    private void OnFormDataPropertyChanged(string propertyPath)
    {
        if (propertyPath == PropertyPath)
        {
            UpdateValue();
        }
    }

    private void OnMemberPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Guard.IsNotNull(FormDataProvider);

        if (e.PropertyName == nameof(Value))
        {
            // Stop listening for component property changes while we update the component
            FormDataProvider.FormPropertyChanged -= OnFormDataPropertyChanged;

            var jsonValue = JsonSerializer.Serialize(Value);

            FormDataProvider.SetProperty(PropertyPath, jsonValue, false);

            // Start listening for component property changes again
            FormDataProvider.FormPropertyChanged += OnFormDataPropertyChanged;
        }
    }

    protected Result UpdateValue()
    {
        Guard.IsNotNull(FormDataProvider);

        // Sync the value member variable with the property
        var getResult = FormDataProvider.GetProperty(PropertyPath);
        if (getResult.IsFailure)
        {
            return Result.Fail($"Failed to get property: '{PropertyPath}'")
                .WithErrors(getResult);
        }
        var jsonValue = getResult.Value;

        var jsonNode = JsonNode.Parse(jsonValue);
        if (jsonNode is null)
        {
            return Result.Fail($"Failed to parse JSON property: '{PropertyPath}'");
        }

        Value = jsonNode.ToString();

        return Result.Ok();
    }

    protected Result<(bool, string)> GetBindingPropertyPath(JsonElement jsonElement, string configPropertyName)
    {
        if (!jsonElement.TryGetProperty(configPropertyName, out var bindingConfig))
        {
            // Call succeeds, but result bool is false to indicate property is not present
            return Result<(bool, string)>.Ok((false, string.Empty));
        }

        var bindingPropertyPath = bindingConfig.GetString();
        if (string.IsNullOrEmpty(bindingPropertyPath))
        {
            return Result<(bool, string)>.Fail($"Binding property '{configPropertyName}' is empty");
        }

        // Parsed the binding info successfully
        return Result<(bool, string)>.Ok((true, bindingPropertyPath));
    }

    protected Result ApplyBinding(
        FrameworkElement frameworkElement,
        DependencyProperty dependencyProperty,
        BindingMode bindingMode,
        string propertyPath)
    {
        if (FormDataProvider is null)
        {
            return Result.Fail($"Form data provider is null");
        }

        try
        {
            PropertyPath = propertyPath;

            // The DataContext will be used automatically as the binding source
            Guard.IsTrue(frameworkElement.DataContext == this);

            // Tell the view model to stop listening for property changes when the view is unloaded
            frameworkElement.Unloaded += (s, e) =>
            {
                var vm = frameworkElement.DataContext as ElementViewModel;
                if (vm is not null)
                {
                    vm.OnViewUnloaded();
                    frameworkElement.DataContext = null;
                }
            };

            // Bind the dependency property to the property view model
            var binding = new Binding()
            {
                Path = new PropertyPath(BoundPropertyName),
                Mode = bindingMode
            };
            frameworkElement.SetBinding(dependencyProperty, binding);
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when applying property binding: '{propertyPath}'. {ex}");
        }

        return Result.Ok();
    }

    protected Result ValidateConfigKeys(JsonElement jsonElement, HashSet<string> validConfigKeys)
    {
        bool valid = true;
        var keys = new List<string>();
        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                var configKey = property.Name;
                if (configKey == "element" ||
                    configKey == "horizontalAlignment" ||
                    configKey == "verticalAlignment" ||
                    configKey == "tooltip" ||
                    configKey == "alignment")
                {
                    // Skip general config properties that apply to all elements
                    continue;
                }

                if (!validConfigKeys.Contains(configKey))
                {
                    return Result.Fail($"Invalid form element property: '{configKey}'");
                }
            }
        }

        return Result.Ok();
    }

    protected Result ApplyAlignmentConfig(FrameworkElement frameworkElement, JsonElement config)
    {
        if (config.TryGetProperty("horizontalAlignment", out var horizontalProperty))
        {
            var horizontalAlignment = horizontalProperty.GetString();
            switch (horizontalAlignment)
            {
                case "Left":
                    frameworkElement.HorizontalAlignment = HorizontalAlignment.Left;
                    break;

                case "Center":
                    frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
                    break;

                case "Right":
                    frameworkElement.HorizontalAlignment = HorizontalAlignment.Right;
                    break;

                case "Stretch":
                    frameworkElement.HorizontalAlignment = HorizontalAlignment.Stretch;
                    break;

                default:
                    return Result.Fail($"Invalid horizontal alignment value: '{horizontalAlignment}'");
            }
        }

        if (config.TryGetProperty("verticalAlignment", out var verticalAlignmentProperty))
        {
            var verticalAlignment = verticalAlignmentProperty.GetString();
            switch (verticalAlignment)
            {
                case "Top":
                    frameworkElement.VerticalAlignment = VerticalAlignment.Top;
                    break;
                case "Center":
                    frameworkElement.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case "Bottom":
                    frameworkElement.VerticalAlignment = VerticalAlignment.Bottom;
                    break;
                case "Stretch":
                    frameworkElement.VerticalAlignment = VerticalAlignment.Stretch;
                    break;
                default:
                    return Result.Fail($"Invalid vertical alignment value: '{verticalAlignment}'");
            }
        }

        return Result.Ok();
    }

    protected void ApplyTooltip(FrameworkElement frameworkElement, JsonElement config)
    {
        if (config.TryGetProperty("tooltip", out var tooltipText))
        {
            ToolTipService.SetToolTip(frameworkElement, tooltipText);
        }
    }
}
