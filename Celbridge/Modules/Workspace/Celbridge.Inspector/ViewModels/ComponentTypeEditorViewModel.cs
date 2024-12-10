using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentTypeEditorViewModel : ObservableObject
{
    private readonly ILogger<ComponentValueEditorViewModel> _logger;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;

    [ObservableProperty]
    private ObservableCollection<string> _componentTypeList = new();

    public ComponentTypeEditorViewModel(
        ILogger<ComponentValueEditorViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        messengerService.Register<ComponentTypeInputTextChangedMessage>(this, OnComponentTypeInputTextChangedMessage);
        messengerService.Register<ComponentTypeEnteredMessage>(this, OnComponentTypeEnteredMessage);

        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;
    }

    private void OnComponentTypeInputTextChangedMessage(object recipient, ComponentTypeInputTextChangedMessage message)
    {
        var inputText = message.ComponentType;

        // Get list of available component types
        var componentTypes = _entityService.ComponentTypes.Keys.ToList();
        componentTypes.Sort();

        var filteredList = new List<string>();

        if (string.IsNullOrEmpty(inputText))
        {
            // Add all component types
            foreach (var name in componentTypes)
            {
                filteredList.Add(name);
            }
        }
        else
        {
            // Add component types that start with the input text first.
            // These should appear at the top of the list.
            foreach (var name in componentTypes)
            {
                if (name.StartsWith(inputText, StringComparison.InvariantCultureIgnoreCase))
                {
                    filteredList.Add(name);
                }
            }

            // Remove any component types that have already been added
            componentTypes.Remove(item => filteredList.Contains(item));

            // Todo: Remove any component types that don't allow multiples of the same type (if there's already an instance)

            // Now add any remaining component types that contain the input text
            foreach (var name in componentTypes)
            {
                if (name.Contains(inputText, StringComparison.InvariantCultureIgnoreCase))
                {
                    filteredList.Add(name);
                }
            }
        }

        if (filteredList.SequenceEqual(ComponentTypeList))
        {
            return;
        }

        ComponentTypeList.ReplaceWith(filteredList);
    }

    private void OnComponentTypeEnteredMessage(object recipient, ComponentTypeEnteredMessage message)
    {
        if (ComponentTypeList.Count == 0)
        {
            // Enter is a noop if no component types are available
            return;
        }

        var newComponentType = ComponentTypeList[0];

        var resource = _inspectorService.InspectedResource;
        var componentIndex = _inspectorService.InspectedComponentIndex;

        var replaceResult = _entityService.ReplaceComponent(resource, componentIndex, newComponentType);
        if (replaceResult.IsFailure)
        {
            _logger.LogError(replaceResult.Error);

            return;
        }        
    }
}
