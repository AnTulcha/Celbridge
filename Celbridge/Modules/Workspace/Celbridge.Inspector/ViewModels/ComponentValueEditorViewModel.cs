using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentValueEditorViewModel : ObservableObject
{
    private readonly ILogger<ComponentValueEditorViewModel> _logger;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;

    [ObservableProperty]
    private string _componentType = string.Empty;

    [ObservableProperty]
    private string _propertyList = string.Empty;

    public ComponentValueEditorViewModel(
        ILogger<ComponentValueEditorViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;

        messengerService.Register<InspectorTargetChangedMessage>(this, OnInspectedResourceChangedMessage);
        messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);
    }

    private void OnInspectedResourceChangedMessage(object recipient, InspectorTargetChangedMessage message)
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
        PropertyList = string.Empty;

        // Handle invalid resource / component index by clearing the displayed properties

        if (resource.IsEmpty)
        {
            ComponentType = string.Empty;
            return;
        }

        if (componentIndex < 0)
        {
            ComponentType = string.Empty;
            return;
        }

        var getCountResult = _entityService.GetComponentCount(resource);
        if (getCountResult.IsFailure)
        {
            _logger.LogError($"Failed to get component count for resource: '{resource}'");
            return;
        }

        int componentCount = getCountResult.Value;
        if (componentIndex >= componentCount)
        {
            ComponentType = string.Empty;
            return;
        }

        // Resource and component index are valid, so get the component info and display the properties

        var getResult = _entityService.GetComponentInfo(resource, componentIndex);
        if (getResult.IsFailure)
        {
            _logger.LogError($"Failed to get component info: {resource}, {componentIndex}");
            return;
        }

        var componentInfo = getResult.Value;

        ComponentType = componentInfo.ComponentType;

        var sb = new StringBuilder();

        // sb.AppendLine($"{resource}, {componentIndex}, {componentInfo.ComponentType}");
        foreach (var property in componentInfo.Properties)
        {
            var getValueResult = _entityService.GetPropertyAsJson(resource, componentIndex, $"/{property.PropertyName}");
            if (getValueResult.IsFailure)
            {
                _logger.LogError($"Failed to get value: {property.PropertyName} ({property.PropertyType})");
                continue;
            }
            var value = getValueResult.Value;

            sb.AppendLine($"{property.PropertyName} ({property.PropertyType}): {value}");
        }

        PropertyList = sb.ToString();
    }
}
