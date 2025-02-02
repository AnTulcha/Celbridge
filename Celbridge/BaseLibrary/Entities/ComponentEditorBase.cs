namespace Celbridge.Entities;

/// <summary>
/// An abstract base class for implementing a component editor.
/// Handles the default binding for viewing and updating component properties.
/// Override the virtual methods to customize the behaviour of the editor.
/// </summary>
public abstract class ComponentEditorBase : IComponentEditor
{
    private IComponentEditorHelper? _helper;

    public event Action<string>? FormPropertyChanged;

    public IComponentProxy Component => _helper!.Component;

    public virtual Result Initialize(IComponentProxy component)
    {
        _helper = ServiceLocator.AcquireService<IComponentEditorHelper>();
        _helper.Initialize(component);
        _helper.ComponentPropertyChanged += OnComponentPropertyChanged;

        return Result.Ok();
    }

    private void OnComponentPropertyChanged(string propertyPath)
    {
        // Forward component property changes to the form
        FormPropertyChanged?.Invoke(propertyPath);
    }

    public abstract string GetComponentConfig();

    public abstract ComponentSummary GetComponentSummary();

    public virtual void OnFormUnloaded()
    {
        Guard.IsNotNull(_helper);

        _helper.ComponentPropertyChanged -= OnComponentPropertyChanged;
        _helper.Uninitialize();
    }

    public string LoadEmbeddedResource(string resourcePath)
    {
        var helper = _helper;

        if (helper is null)
        {
            // This happens when the config registry is populated during startup.
            // We can't initalize the helper because there's no component, but that's fine
            // because we only need to access LoadEmbeddedResource()
            helper = ServiceLocator.AcquireService<IComponentEditorHelper>();
        }

        // Get the type of the component editor class.
        // The embedded resource must be in the same assembly as this type.
        return helper.LoadEmbeddedResource(GetType(), resourcePath);
    }

    public Result<string> GetPropertyAsJson(string propertyPath)
    {
        return Component.GetPropertyAsJson(propertyPath);
    }

    public Result SetPropertyAsJson(string propertyPath, string jsonValue, bool insert = false)
    {
        return Component.SetPropertyAsJson(propertyPath, jsonValue, insert);
    }
}
