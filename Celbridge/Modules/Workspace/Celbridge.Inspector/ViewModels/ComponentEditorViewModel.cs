using System.ComponentModel;
using System.Dynamic;
using Celbridge.Entities;
using Celbridge.Logging;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Inspector.ViewModels;

public class ComponentEditorViewModel : DynamicObject, INotifyPropertyChanged
{
    private readonly ILogger<ComponentEditorViewModel> _logger;
    private IComponentEditor? _componentEditor;

    public ComponentEditorViewModel(
        ILogger<ComponentEditorViewModel> logger)
    {
        _logger = logger;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Initialize(IComponentEditor componentEditor)
    {
        _componentEditor = componentEditor;
    }

    // Indexer to get or set dynamic properties
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        Guard.IsNotNull(_componentEditor);

        var getPropertyResult = _componentEditor.GetProperty(binder.Name);
        if (getPropertyResult.IsFailure)
        {
            _logger.LogError($"Failed to get bound property: {binder.Name}");
            result = null;
            return false;
        }

        result = getPropertyResult.Value;
        return result != null;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        Guard.IsNotNull(_componentEditor);

        if (value is null)
        {
            _logger.LogError($"Setting a bound property to null is not supported: {binder.Name}");
            return false;
        }

        var setPropertyResult = _componentEditor.SetProperty(binder.Name, value);
        if (setPropertyResult.IsFailure)
        {
            _logger.LogError($"Failed to set bound property: {binder.Name}");
            return false;
        }
        var changedValue = setPropertyResult.Value;

        if (changedValue)
        {
            OnPropertyChanged(binder.Name);
        }

        return true;
    }

    // Raises the PropertyChanged event
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
