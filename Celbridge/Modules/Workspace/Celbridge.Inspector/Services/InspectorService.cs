using Celbridge.Explorer;
using Celbridge.Messaging;
using Celbridge.Workspace;

namespace Celbridge.Inspector.Services;

public class InspectorService : IInspectorService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;

    private IInspectorPanel? _inspectorPanel;
    public IInspectorPanel InspectorPanel => _inspectorPanel!;

    public IInspectorFactory InspectorFactory { get; }

    public ResourceKey InspectedResource { get; private set; }

    public int InspectedComponentIndex {  get; private set; }

    public InspectorService(
        IServiceProvider serviceProvider,
        IMessengerService messengerService)
    {
        _serviceProvider = serviceProvider;
        _messengerService = messengerService;

        _messengerService.Register<WorkspaceWillPopulatePanelsMessage>(this, OnWorkspaceWillPopulatePanelsMessage);
        _messengerService.Register<SelectedResourceChangedMessage>(this, OnSelectedResourceChangedMessage);
        _messengerService.Register<SelectedComponentChangedMessage>(this, OnSelectedComponentChangedMessage);

        InspectorFactory = _serviceProvider.GetRequiredService<IInspectorFactory>();
    }

    private void OnWorkspaceWillPopulatePanelsMessage(object recipient, WorkspaceWillPopulatePanelsMessage message)
    {
        _inspectorPanel = _serviceProvider.GetRequiredService<IInspectorPanel>();
    }

    private void OnSelectedResourceChangedMessage(object recipient, SelectedResourceChangedMessage message)
    {
        InspectedResource = message.Resource;
        InspectedComponentIndex = -1;

        var changedMessage = new InspectorTargetChangedMessage(InspectedResource, InspectedComponentIndex);
        _messengerService.Send(changedMessage);
    }

    private void OnSelectedComponentChangedMessage(object recipient, SelectedComponentChangedMessage message)
    {
        InspectedComponentIndex = message.componentIndex;

        var changedMessage = new InspectorTargetChangedMessage(InspectedResource, InspectedComponentIndex);
        _messengerService.Send(changedMessage);
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed objects here
                _messengerService.Unregister<WorkspaceWillPopulatePanelsMessage>(this);
            }

            _disposed = true;
        }
    }

    ~InspectorService()
    {
        Dispose(false);
    }
}
