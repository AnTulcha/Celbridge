namespace Celbridge.Entities.Services;

public class ComponentEditorHelper : IComponentEditorHelper
{
    public event Action<string>? ComponentPropertyChanged;

    protected IComponentProxy? _component;
    public IComponentProxy Component => _component!;

    public virtual Result Initialize(IComponentProxy component)
    {
        _component = component;
        _component.ComponentPropertyChanged += OnComponentPropertyChanged;

        return Result.Ok();
    }

    public virtual void Uninitialize()
    {
        if (_component is not null)
        {
            _component.ComponentPropertyChanged -= OnComponentPropertyChanged;
        }
    }

    protected virtual void OnComponentPropertyChanged(string propertyPath)
    {
        // Forward the property changed event so the editor view can update itself
        ComponentPropertyChanged?.Invoke(propertyPath);
    }
}
