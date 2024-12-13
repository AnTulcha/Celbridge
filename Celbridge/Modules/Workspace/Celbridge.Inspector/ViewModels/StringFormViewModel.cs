using System.ComponentModel;
using Celbridge.Entities;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class StringFormViewModel : ObservableObject
{
    private IEntityService _entityService;

    public ResourceKey Resource { get; private set; }

    public int ComponentIndex { get; private set; }

    public string PropertyName { get; private set; } = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    private bool _suppressPropertyChange = false;

    public StringFormViewModel(
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _entityService = workspaceWrapper.WorkspaceService.EntityService;

        messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);
    }

    public Result Initialize(ResourceKey resource, int componentIndex, string propertyName)
    {
        // Initialize should only be called once
        Guard.IsTrue(Resource.IsEmpty);

        Resource = resource;
        ComponentIndex = componentIndex;
        PropertyName = propertyName;

        ReadPropertyValue();

        PropertyChanged += OnPropertyChanged;

        return Result.Ok();
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        if (message.Resource == Resource && 
            message.ComponentIndex == ComponentIndex &&
            message.PropertyPath == $"/{PropertyName}")
        {
            ReadPropertyValue();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressPropertyChange)
        {
            return;
        }

        if (e.PropertyName == nameof(Value))
        {
            WritePropertyValue();
        }
    }

    private void ReadPropertyValue()
    {
        var getResult = _entityService.GetProperty<string>(Resource, ComponentIndex, PropertyName);
        if (getResult.IsFailure)
        {
            return;
        }

        _suppressPropertyChange = true;

        var stringValue = getResult.Value;
        Value = stringValue;

        _suppressPropertyChange = false;
    }

    private void WritePropertyValue()
    {
        _suppressPropertyChange = true;

        var setResult = _entityService.SetProperty(Resource, ComponentIndex, PropertyName, Value);
        if (setResult.IsFailure)
        {
            return;
        }

        _suppressPropertyChange = false;
    }
}
