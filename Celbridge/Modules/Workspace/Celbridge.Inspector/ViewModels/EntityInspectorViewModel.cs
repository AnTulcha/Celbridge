using Celbridge.Entities;
using Celbridge.Inspector.Models;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using System.Collections.ObjectModel;

namespace Celbridge.Inspector.ViewModels;

public partial class EntityInspectorViewModel : InspectorViewModel
{
    private readonly ILogger<MarkdownInspectorViewModel> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    public ObservableCollection<ComponentItem> ComponentItems { get; } = new();

    // Code gen requires a parameterless constructor
    public EntityInspectorViewModel()
    {
        throw new NotImplementedException();
    }

    public EntityInspectorViewModel(
        ILogger<MarkdownInspectorViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;

        _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);

        PropertyChanged += EntityInspectorViewModel_PropertyChanged;
    }

    public void OnViewLoaded()
    {
        // Todo: Populate the component items list with the components of the entity

        ComponentItems.Add(new ComponentItem { ComponentType = "Component 1" });
        ComponentItems.Add(new ComponentItem { ComponentType = "Component 2" });
        ComponentItems.Add(new ComponentItem { ComponentType = "Component 3" });
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        if (message.Resource == Resource)
        {
            // Todo: Update component list
        }
    }

    private void EntityInspectorViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Resource))
        {
            // Todo: Update component list
        }
    }
}
