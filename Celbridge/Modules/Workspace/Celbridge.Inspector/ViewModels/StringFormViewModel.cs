using System.ComponentModel;
using Celbridge.Entities;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class StringFormViewModel : ObservableObject
{
    private IMessengerService _messengerService;
    private IEntityService _entityService;

    public ResourceKey Resource { get; private set; }

    public int ComponentIndex { get; private set; }

    public string PropertyName { get; private set; } = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    private bool _ignoreComponentChangeMessage = false;
    private bool _ignoreValueChange = false;

    public StringFormViewModel(
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
    }

    public Result Initialize(ResourceKey resource, int componentIndex, string propertyName)
    {
        // Initialize should only be called once
        Guard.IsTrue(Resource.IsEmpty);

        Resource = resource;
        ComponentIndex = componentIndex;
        PropertyName = propertyName;

        // Todo: Use humanizer to format the property name header

        // Read property into Value
        ReadProperty();

        // Listening for component property changes
        _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);

        // Listen for value changes (i.e. user entered text)
        PropertyChanged += OnPropertyChanged;

        return Result.Ok();
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        if (_ignoreComponentChangeMessage)
        {
            return;
        }

        if (message.Resource == Resource && 
            message.ComponentIndex == ComponentIndex &&
            message.PropertyPath == $"/{PropertyName}")
        {
            ReadProperty();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_ignoreValueChange)
        {
            return;
        }

        if (e.PropertyName == nameof(Value))
        {
            // Update the property, but ignore the next component change message
            _ignoreComponentChangeMessage = true;
            WriteProperty();
            _ignoreComponentChangeMessage = false;
        }
    }

    private void ReadProperty()
    {
        var getResult = _entityService.GetProperty<string>(Resource, ComponentIndex, PropertyName);
        if (getResult.IsFailure)
        {
            return;
        }

        var stringValue = getResult.Value;

        _ignoreValueChange = true;
        Value = stringValue;
        _ignoreValueChange = false;
    }

    private void WriteProperty()
    {
        _entityService.SetProperty(Resource, ComponentIndex, PropertyName, Value);
    }
}
