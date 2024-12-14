using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentValueEditorViewModel : ObservableObject
{
    private readonly ILogger<ComponentValueEditorViewModel> _logger;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;

    [ObservableProperty]
    private string _componentType = string.Empty;

    public event Action<List<IForm>>? OnFormCreated;

    public ComponentValueEditorViewModel(
        ILogger<ComponentValueEditorViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;

        messengerService.Register<InspectedComponentChangedMessage>(this, OnInspectedResourceChangedMessage);
        messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);
    }

    private void OnInspectedResourceChangedMessage(object recipient, InspectedComponentChangedMessage message)
    {
        var (resource, componentIndex) = message;
        PopulatePropertyList(resource, componentIndex);
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        var resource = message.Resource;
        var componentIndex = message.ComponentIndex;

        if (_inspectorService.InspectedResource == resource &&
            _inspectorService.InspectedComponentIndex == componentIndex)
        {
            PopulatePropertyList(resource, componentIndex);
        }
    }

    private void PopulatePropertyList(ResourceKey resource, int componentIndex)
    {
        // Handle invalid resource / component index by clearing the UI and returning

        List<IForm> propertyForms = new();

        if (resource.IsEmpty || 
            componentIndex < 0)
        {
            _logger.LogError($"Invalid resource or component index");

            ComponentType = string.Empty;
            OnFormCreated?.Invoke(propertyForms);
            return;
        }

        var getCountResult = _entityService.GetComponentCount(resource);
        if (getCountResult.IsFailure)
        {
            _logger.LogError($"Failed to get component count for resource: '{resource}'");

            ComponentType = string.Empty;
            OnFormCreated?.Invoke(propertyForms);
            return;
        }

        int componentCount = getCountResult.Value;
        if (componentIndex >= componentCount)
        {
            _logger.LogError($"Component index '{componentIndex}' is out of range for resource '{resource}'");

            ComponentType = string.Empty;
            OnFormCreated?.Invoke(propertyForms);
            return;
        }

        // Resource and component index are valid, so get the component info and display the properties

        var getResult = _entityService.GetComponentTypeInfo(resource, componentIndex);
        if (getResult.IsFailure)
        {
            _logger.LogError($"Failed to get component info for resource '{resource}' at index '{componentIndex}'");

            ComponentType = string.Empty;
            OnFormCreated?.Invoke(propertyForms);
            return;
        }

        // Populate the Component Type in the panel header

        var componentTypeInfo = getResult.Value;
        ComponentType = componentTypeInfo.ComponentType;

        // Populate the property form

        foreach (var property in componentTypeInfo.Properties)
        {
            var createResult = _inspectorService.FormFactory.CreatePropertyForm(resource, componentIndex, property.PropertyName);
            if (createResult.IsFailure)
            {
                _logger.LogError($"Failed to create form for property '{property.PropertyName}' in resource '{resource}' at component index '{componentIndex}'");
                continue;
            }
            var form = createResult.Value;

            propertyForms.Add(form);
        }

        OnFormCreated?.Invoke(propertyForms);
    }
}
