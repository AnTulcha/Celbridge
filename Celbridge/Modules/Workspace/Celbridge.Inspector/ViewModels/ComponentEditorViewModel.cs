using System.Text;
using Celbridge.Entities;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentEditorViewModel : ObservableObject
{
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;

    [ObservableProperty]
    private string _inspectedItem = string.Empty;

    public ComponentEditorViewModel(
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
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
        if (resource.IsEmpty)
        {
            InspectedItem = "Resource: None";
            return;
        }

        if (componentIndex < 0)
        {
            InspectedItem = $"Resource: {resource}";
            return;
        }

        var getResult = _entityService.GetComponentInfo(resource, componentIndex);
        if (getResult.IsFailure)
        {
            InspectedItem = $"Failed to get component info: {resource}, {componentIndex}";
            return;
        }

        var componentInfo = getResult.Value;

        var sb = new StringBuilder();

        sb.AppendLine($"{resource}, {componentIndex}, {componentInfo.ComponentType}");
        foreach (var property in componentInfo.Properties)
        {
            var getValueResult = _entityService.GetPropertyAsJson(resource, componentIndex, $"/{property.PropertyName}");
            if (getValueResult.IsFailure)
            {
                sb.AppendLine($"{property.PropertyName} ({property.PropertyType}): Failed to get value");
                continue;
            }
            var value = getValueResult.Value;

            sb.AppendLine($"{property.PropertyName} ({property.PropertyType}): {value}");
        }

        InspectedItem = sb.ToString();
    }
}
