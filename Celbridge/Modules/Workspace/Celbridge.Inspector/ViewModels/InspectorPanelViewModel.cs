using Celbridge.Explorer;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class InspectorPanelViewModel : ObservableObject
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    [ObservableProperty]
    private ResourceKey _selectedResource;

    public InspectorPanelViewModel()
    {
        throw new NotImplementedException();
    }

    public InspectorPanelViewModel(
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;

        messengerService.Register<SelectedResourceChangedMessage>(this, OnSelectedResourceChangedMessage);
    }

    private void OnSelectedResourceChangedMessage(object recipient, SelectedResourceChangedMessage message)
    {
        SelectedResource = message.Resource;
    }

}
