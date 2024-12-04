using Celbridge.Commands;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class EntityInspectorViewModel : InspectorViewModel
{
    private readonly ILogger<MarkdownInspectorViewModel> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    [ObservableProperty]
    private EditorMode _editorMode;

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
