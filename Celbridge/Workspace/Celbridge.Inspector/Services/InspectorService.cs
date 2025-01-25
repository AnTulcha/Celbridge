using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Forms;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;

namespace Celbridge.Inspector.Services;

/// <summary>
/// Message sent when the workspace requests the inspector to update.
/// </summary>
public record UpdateInspectorMessage();

public class InspectorService : IInspectorService, IDisposable
{
    private readonly ILogger<InspectorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;
    private readonly IFormService _formService;

    private IInspectorPanel? _inspectorPanel;
    public IInspectorPanel InspectorPanel => _inspectorPanel!;

    public IInspectorFactory InspectorFactory { get; }

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
        IMessengerService messengerService,
        IFormService formService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messengerService = messengerService;
        _formService = formService;

        _messengerService.Register<WorkspaceWillPopulatePanelsMessage>(this, OnWorkspaceWillPopulatePanelsMessage);
        _messengerService.Register<SelectedResourceChangedMessage>(this, OnSelectedResourceChangedMessage);
        _messengerService.Register<SelectedComponentChangedMessage>(this, OnSelectedComponentChangedMessage);

        InspectorFactory = _serviceProvider.GetRequiredService<IInspectorFactory>();
        _formService = formService;
    }

    public async Task<Result> UpdateAsync()
    {
        await Task.CompletedTask;

        // Notify the inspector panel elements to update
        var message = new UpdateInspectorMessage();
        _messengerService.Send(message);

        return Result.Ok();
    }

    public Result<object> CreateComponentEditorForm(IComponentEditor componentEditor)
    {
        Guard.IsNotNull(componentEditor.Component);

        // The registered form name is just the component type
        var formName = componentEditor.Component.Schema.ComponentType;

        var buildResult = _formService.CreateRegisteredForm(formName, componentEditor);
        if (buildResult.IsFailure)
        {
            return Result<object>.Fail($"Failed to build form for component type: '{formName}'")
                .WithErrors(buildResult);
        }
        var uiElement = buildResult.Value;

        return Result<object>.Ok(uiElement);
    }

    private void OnWorkspaceWillPopulatePanelsMessage(object recipient, WorkspaceWillPopulatePanelsMessage message)
    {
        _inspectorPanel = _serviceProvider.GetRequiredService<IInspectorPanel>();
    }

    private void OnSelectedResourceChangedMessage(object recipient, SelectedResourceChangedMessage message)
    {
        InspectedResource = message.Resource;
        InspectedComponentIndex = -1;

        var changedMessage = new InspectedComponentChangedMessage(new ComponentKey(InspectedResource, InspectedComponentIndex));
        _messengerService.Send(changedMessage);
    }

    private void OnSelectedComponentChangedMessage(object recipient, SelectedComponentChangedMessage message)
    {
        InspectedComponentIndex = message.componentIndex;

        var changedMessage = new InspectedComponentChangedMessage(new ComponentKey(InspectedResource, InspectedComponentIndex));
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
                _messengerService.UnregisterAll(this);
            }

            _disposed = true;
        }
    }

    ~InspectorService()
    {
        Dispose(false);
    }
}
