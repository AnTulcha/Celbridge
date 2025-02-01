namespace Celbridge.Entities;

/// <summary>
/// An abstract base class for implementing a component editor.
/// Handles the default binding for viewing and updating component properties.
/// Override the virtual methods to customize the behaviour of the editor.
/// </summary>
public abstract class ComponentEditorBase : IComponentEditor
{
    public abstract string ComponentConfigPath { get; }
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

    public abstract ComponentSummary GetComponentSummary();

    public virtual void OnFormUnloaded()
    {
        Guard.IsNotNull(_helper);

        _helper.ComponentPropertyChanged -= OnComponentPropertyChanged;
        _helper.Uninitialize();
    }

    public string GetString(string propertyPath) => Component.GetString(propertyPath);

    public virtual Result<string> GetProperty(string propertyPath)
    {
        return Component.GetProperty<string>(propertyPath);
    }

    public virtual Result SetProperty(string propertyPath, string newValue, bool insert)
    {
        return Component.SetProperty<string>(propertyPath, newValue, insert);
    }
}
