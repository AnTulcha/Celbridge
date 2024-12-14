using System.ComponentModel;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class StringFormViewModel : ObservableObject
{
    private ILogger<StringFormViewModel> _logger;
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
        ILogger<StringFormViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
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

        // Start listening for component changes and value changes
        _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);
        PropertyChanged += OnPropertyChanged;

        // Listen for value changes (i.e. user entered text)

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
            // _logger.LogInformation("Component changed");
    
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
            // _logger.LogInformation("Value changed");

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

    public void OnViewUnloaded()
    {
        // Unregister event/message handlers
        _messengerService.Unregister<ComponentChangedMessage>(this);
        PropertyChanged -= OnPropertyChanged;
    }
}
