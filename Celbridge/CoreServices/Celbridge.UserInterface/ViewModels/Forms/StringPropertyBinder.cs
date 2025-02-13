using System.Text.Json;
using System.Text.Json.Nodes;

namespace Celbridge.UserInterface.ViewModels.Forms;

public class StringPropertyBinder
{
    private string _propertyPath = string.Empty;

    private FrameworkElement _frameworkElement;
    private FormElement _formElement;

    private DependencyProperty? _dependencyProperty;
    private BindingMode _bindingMode;
    private string _memberName = string.Empty;

    private Action<string>? _setterAction;
    private Func<string>? _getterAction;

    private StringPropertyBinder(FrameworkElement frameworkElement, FormElement formElement)
    {
        _frameworkElement = frameworkElement;
        _formElement = formElement;
    }

    public static StringPropertyBinder Create(
        FrameworkElement frameworkElement, 
        FormElement formElement)
    {
        var propertyBinder = new StringPropertyBinder(frameworkElement, formElement);
        return propertyBinder;
    }

    public StringPropertyBinder Binding(
        DependencyProperty dependencyProperty,
        BindingMode bindingMode,
        string memberName)
    {
        _dependencyProperty = dependencyProperty;
        _bindingMode = bindingMode;
        _memberName = memberName;
        return this;
    }

    public StringPropertyBinder Setter(Action<string> setterAction)
    {
        _setterAction = setterAction;
        return this;
    }

    public StringPropertyBinder Getter(Func<string> getterAction)
    {
        _getterAction = getterAction;
        return this;
    }

    public Result Initialize(JsonElement config, string configKey)
    {
        if (config.TryGetProperty(configKey, out var stringValue))
        {
            // Check the type
            if (stringValue.ValueKind != JsonValueKind.String)
            {
                return Result.Fail($"'{configKey}' property must be a string");
            }

            // Apply the property
            var stringText = stringValue.GetString()!;
            if (stringText.StartsWith('/'))
            {
                // Store the property path for future updates
                _propertyPath = stringText;

                if (_dependencyProperty is not null)
                {
                    // Bind dependency property to a member variable on the form element class
                    _frameworkElement.SetBinding(_dependencyProperty, new Binding()
                    {
                        Path = new PropertyPath(_memberName),
                        Mode = _bindingMode
                    });
                }

                // Sync the member variable with the form data provider
                UpdatePropertyValue();

                // Set a flag to indicate that this element has bindings
                _formElement.HasBindings = true;
            }
            else
            {
                // Set member property directly (i.e no binding)
                // Todo: Support localization
                _setterAction?.Invoke(stringText);
            }
        }

        return Result.Ok();
    }

    public Result UpdatePropertyValue()
    {
        var formDataProvider = _formElement.FormDataProvider;

        // Read the current property JSON value via the FormDataProvider
        var getResult = formDataProvider.GetProperty(_propertyPath);
        if (getResult.IsFailure)
        {
            return Result.Fail($"Failed to get property: '{_propertyPath}'")
                .WithErrors(getResult);
        }
        var jsonValue = getResult.Value;

        // Parse the JSON value as a string
        var jsonNode = JsonNode.Parse(jsonValue);
        if (jsonNode is null)
        {
            return Result.Fail($"Failed to parse JSON value for property: '{_propertyPath}'");
        }
        var value = jsonNode.ToString();

        // Update the member variable
        _setterAction?.Invoke(value);

        return Result.Ok();
    }

    public void OnFormDataChanged(string propertyPath)
    {
        if (string.IsNullOrEmpty(_propertyPath) ||
            propertyPath != _propertyPath)
        {
            return;
        }

        UpdatePropertyValue();
    }

    public void OnMemberDataChanged(string propertyName)
    {
        if (_getterAction is null ||
            string.IsNullOrEmpty(_memberName) ||
            propertyName != _memberName)
        {
            return;
        }

        var memberValue = _getterAction?.Invoke();

        var jsonValue = JsonSerializer.Serialize(memberValue);

        var formDataProvider = _formElement.FormDataProvider;
        formDataProvider.SetProperty(_propertyPath, jsonValue, false);
    }
}
