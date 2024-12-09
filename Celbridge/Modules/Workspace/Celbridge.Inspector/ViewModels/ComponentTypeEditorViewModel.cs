using System.Collections.ObjectModel;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentTypeEditorViewModel : ObservableObject
{
    private readonly ILogger<ComponentValueEditorViewModel> _logger;
    private readonly IEntityService _entityService;

    [ObservableProperty]
    private ObservableCollection<string> _componentTypeList = new();

    public ComponentTypeEditorViewModel(
        ILogger<ComponentValueEditorViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        messengerService.Register<ComponentTypeInputChangedMessage>(this, OnComponentTypeInputChangedMessage);

        _entityService = workspaceWrapper.WorkspaceService.EntityService;
    }

    private void OnComponentTypeInputChangedMessage(object recipient, ComponentTypeInputChangedMessage message)
    {
        var componentType = message.ComponentType;

        ComponentTypeList.Clear();
        ComponentTypeList.Add(componentType);
    }
}
