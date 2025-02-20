using Celbridge.Entities;
using Celbridge.UserInterface.Services.Forms;
using System.Text.Json;

namespace Celbridge.UserInterface.ViewModels.Forms;

public partial class ComboBoxElement : FormElement
{
    public static Result<FrameworkElement> CreateComboBox(JsonElement config, FormBuilder formBuilder)
    {
        var formElement = ServiceLocator.AcquireService<ComboBoxElement>();
        return formElement.Create(config, formBuilder);
    }

    private PropertyBinder<bool>? _isEnabledBinder;

    [ObservableProperty]
    private string _selectedKey = string.Empty;
    private PropertyBinder<string>? _selectedKeyBinder;

    [ObservableProperty]
    private List<string> _values = new();
    private PropertyBinder<List<string>>? _valuesBinder;

    protected override Result<FrameworkElement> CreateUIElement(JsonElement config, FormBuilder formBuilder)
    {
        //
        // Create the UI element
        //

        var comboBox = new ComboBox();
        comboBox.DataContext = this;

        //
        // Check all specified config properties are supported
        //

        var validateResult = ValidateConfigKeys(config, new HashSet<string>()
        {
            "isEnabled",
            "header",
            "item",
            "values"
        });

        if (validateResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail("Invalid ComboBox configuration")
                .WithErrors(validateResult);
        }

        //
        // Apply common config properties
        //

        var commonConfigResult = ApplyCommonConfig(comboBox, config);
        if (commonConfigResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail($"Failed to apply common config properties")
                .WithErrors(commonConfigResult);
        }

        //
        // Apply element-specific config properties
        //

        var isEnabledResult = ApplyIsEnabledConfig(config, comboBox);
        if (isEnabledResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail($"Failed to apply 'isEnabled' config")
                .WithErrors(isEnabledResult);
        }

        var headerResult = ApplyHeaderConfig(config, comboBox);
        if (headerResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail($"Failed to apply 'header' config property")
                .WithErrors(headerResult);
        }

        var valuesResult = ApplyValuesConfig(config, comboBox);
        if (valuesResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail($"Failed to apply 'values' config property")
                .WithErrors(valuesResult);
        }

        var itemResult = ApplyItemConfig(config, comboBox, formBuilder);
        if (itemResult.IsFailure)
        {
            return Result<FrameworkElement>.Fail($"Failed to apply 'item' config property")
                .WithErrors(itemResult);
        }

        return Result<FrameworkElement>.Ok(comboBox);
    }

    private Result ApplyIsEnabledConfig(JsonElement config, ComboBox button)
    {
        if (config.TryGetProperty("isEnabled", out var configValue))
        {
            if (PropertyBinder<bool>.IsBindingConfig(configValue))
            {
                _isEnabledBinder = PropertyBinder<bool>.Create(button, this)
                    .Setter((value) =>
                    {
                        button.IsEnabled = value;
                    });

                return _isEnabledBinder.Initialize(configValue);
            }
            else if (configValue.ValueKind == JsonValueKind.True)
            {
                button.IsEnabled = true;
            }
            else if (configValue.ValueKind == JsonValueKind.False)
            {
                button.IsEnabled = false;
            }
            else
            {
                return Result<bool>.Fail("'isEnabled' config is not valid");
            }
        }

        return Result.Ok();
    }

    private Result ApplyHeaderConfig(JsonElement config, ComboBox comboBox)
    {
        if (config.TryGetProperty("header", out var jsonValue))
        {
            // Check the type
            if (jsonValue.ValueKind != JsonValueKind.String)
            {
                return Result.Fail("'header' property must be a string");
            }

            // Todo: Support binding

            // Apply the property
            var header = jsonValue.GetString() ?? string.Empty;
            comboBox.Header = header;
        }

        return Result.Ok();
    }

    private Result ApplyValuesConfig(JsonElement config, ComboBox comboBox)
    {
        if (config.TryGetProperty("values", out var configValue))
        {
            if (configValue.ValueKind == JsonValueKind.Array)
            {
                var enumValues = JsonSerializer.Deserialize<List<string>>(configValue.GetRawText());
                if (enumValues is null)
                {
                    return Result.Fail($"Failed to deserialize 'values' property");
                }

                if (enumValues.Count != enumValues.Distinct().Count())
                {
                    return Result.Fail($"'values' property contains duplicate items");
                }

                Values.ReplaceWith(enumValues);

                return Result.Ok();
            }
            else if (configValue.ValueKind == JsonValueKind.String)
            {
                // If the 'values' property specifies a binding then apply the binding.
                if (PropertyBinder<List<string>>.IsBindingConfig(configValue))
                {
                    _valuesBinder = PropertyBinder<List<string>>.Create(comboBox, this)
                        .Binding(ComboBox.ItemsSourceProperty,
                            BindingMode.OneWay,
                            nameof(Values))
                        .Setter((value) =>
                        {
                            Values = value;
                        });

                    return _valuesBinder.Initialize(configValue);
                }
            }

            return Result.Fail($"Failed to apply binding for 'values' property");
        }

        return Result.Ok();
    }

    private Result ApplyItemConfig(JsonElement config, ComboBox comboBox, FormBuilder formBuilder)
    {
        if (config.TryGetProperty("item", out var configValue))
        {
            // Check the type
            if (configValue.ValueKind != JsonValueKind.String)
            {
                return Result.Fail("'item' property must be a string");
            }

            // The ItemSource must be populated before we apply the binding so that the item can be
            // selected during initialization.

            // Acquire the component that we are binding to.
            var componentEditor = formBuilder.FormDataProvider as IComponentEditor;
            if (componentEditor is null)
            {
                // ComboBox may only be bound to a component property, so the IFormDataProvider
                // must also be an IComponentEditor.
                return Result.Fail("Failed to acquire component for binding 'item' property");
            }
            var component = componentEditor.Component;

            // Acquire the property and check if it specifies enum values.
            var propertyPath = configValue.ToString();
            var property = component.Schema.Properties.First((p) => propertyPath == $"/{p.PropertyName}");
            if (property is null)
            {
                return Result.Fail($"Failed to acquire component property '{property}'");
            }

            if (property.Attributes.TryGetValue("enum", out var enumJson))
            {
                // The property specifies an enum attribute, so use the enum values as the ItemSource.
                var enumValues = JsonSerializer.Deserialize<List<string>>(enumJson);
                if (enumValues is null)
                {
                    return Result.Fail($"Failed to deserialize enum json");
                }

                if (enumValues.Count != enumValues.Distinct().Count())
                {
                    return Result.Fail($"'values' property contains duplicate items");
                }

                comboBox.ItemsSource = enumValues;
            }
            else
            {
                // Apply the values read from the 'values' config we read earlier.
                // If no values were specified then _values will be empty.
                comboBox.ItemsSource = Values;
            }

            if (PropertyBinder<bool>.IsBindingConfig(configValue))
            {
                _selectedKeyBinder = PropertyBinder<string>.Create(comboBox, this)
                    .Binding(ComboBox.SelectedValueProperty,
                        BindingMode.TwoWay,
                        nameof(SelectedKey))
                    .Setter((value) =>
                    {
                        SelectedKey = value;
                    })
                    .Getter(() =>
                    {
                        return SelectedKey;
                    });

                return _selectedKeyBinder.Initialize(configValue);
            }
            else
            {
                return Result<bool>.Fail("'selectedItem' config is not valid");
            }
        }

        return Result.Ok();
    }

    protected override void OnFormDataChanged(string propertyPath)
    {
        _isEnabledBinder?.OnFormDataChanged(propertyPath);
        _selectedKeyBinder?.OnFormDataChanged(propertyPath);
    }

    protected override void OnMemberDataChanged(string propertyName)
    {
        _selectedKeyBinder?.OnMemberDataChanged(propertyName);
    }

    protected override void OnElementUnloaded()
    {
        _isEnabledBinder?.OnElementUnloaded();
        _selectedKeyBinder?.OnElementUnloaded();
    }

    private void OnButtonClicked(string buttonId)
    {
        FormDataProvider?.OnButtonClicked(buttonId);
    }
}
