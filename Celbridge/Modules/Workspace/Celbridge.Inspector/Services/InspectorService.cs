using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;

namespace Celbridge.Inspector.Services;

public class InspectorService : IInspectorService, IDisposable
{
    private readonly ILogger<InspectorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;

    private IInspectorPanel? _inspectorPanel;
    public IInspectorPanel InspectorPanel => _inspectorPanel!;

    public IInspectorFactory InspectorFactory { get; }

    public IFormFactory FormFactory { get; }

    public ResourceKey InspectedResource { get; private set; }

    public int InspectedComponentIndex {  get; private set; }

    private ComponentPanelMode _componentPanelMode;
    public ComponentPanelMode ComponentPanelMode 
    {
        get => _componentPanelMode;
        set
        {
            if (ComponentPanelMode == value)
            {
                return;
            }

            _componentPanelMode = value;

            var message = new ComponentPanelModeChangedMessage(_componentPanelMode);
            _messengerService.Send(message);
        }
    }

    public InspectorService(
        IServiceProvider serviceProvider,
        ILogger<InspectorService> logger,
        IMessengerService messengerService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messengerService = messengerService;

        _messengerService.Register<WorkspaceWillPopulatePanelsMessage>(this, OnWorkspaceWillPopulatePanelsMessage);
        _messengerService.Register<SelectedResourceChangedMessage>(this, OnSelectedResourceChangedMessage);
        _messengerService.Register<SelectedComponentChangedMessage>(this, OnSelectedComponentChangedMessage);

        InspectorFactory = _serviceProvider.GetRequiredService<IInspectorFactory>();
        FormFactory = _serviceProvider.GetRequiredService<IFormFactory>();
    }

    private void OnWorkspaceWillPopulatePanelsMessage(object recipient, WorkspaceWillPopulatePanelsMessage message)
    {
        _inspectorPanel = _serviceProvider.GetRequiredService<IInspectorPanel>();
    }

    private void OnSelectedResourceChangedMessage(object recipient, SelectedResourceChangedMessage message)
    {
        InspectedResource = message.Resource;
        InspectedComponentIndex = -1;

        var changedMessage = new InspectedComponentChangedMessage(InspectedResource, InspectedComponentIndex);
        _messengerService.Send(changedMessage);
    }

    private void OnSelectedComponentChangedMessage(object recipient, SelectedComponentChangedMessage message)
    {
        InspectedComponentIndex = message.componentIndex;

        var changedMessage = new InspectedComponentChangedMessage(InspectedResource, InspectedComponentIndex);
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
