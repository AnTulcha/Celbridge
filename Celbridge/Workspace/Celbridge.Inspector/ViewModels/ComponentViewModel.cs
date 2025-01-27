using System.ComponentModel;
using Celbridge.Entities;
using Celbridge.Inspector.Services;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class ComponentViewModel : ObservableObject
{
    private readonly ILogger<ComponentViewModel> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    [ObservableProperty]
    private ComponentKey _componentKey;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    private IComponentEditor? _componentEditor;

    private bool _pendingEditorUpdate;
    private bool _pendingSummaryUpdate;

    public ComponentViewModel(
        ILogger<ComponentViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;

        // Listen for the ComponentKey property to initialize so that we can display the correct data at startup.
        // After that, we only need to listen for changes to the entity / component.
        PropertyChanged += OnPropertyChanged;
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ComponentKey))
            {
                UpdateComponentEditor();
                UpdateSummaryProperties();

                // Todo: Unregister these when the view unloads
                _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);
                _messengerService.Register<UpdateInspectorMessage>(this, OnUpdateInspectorMessage);

                PropertyChanged -= OnPropertyChanged;
            }
        }
    }

    public void OnViewUnloaded()
    {
        _componentEditor = null;
        _messengerService.UnregisterAll(this);
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        if (message.ComponentKey.Resource == ComponentKey.Resource)
        {
            if (message.PropertyPath == "/")
            {
                // Recreate the ComponentEditor on any structural change to the entity.
                _pendingEditorUpdate = true;
            }

            // Update the summary when the entity, or any of its components, changes.
            _pendingSummaryUpdate = true;
        }
    }

    private void OnUpdateInspectorMessage(object recipient, UpdateInspectorMessage message)
    {
        if (_pendingEditorUpdate)
        {
            UpdateComponentEditor();
            _pendingEditorUpdate = false;
        }

        if (_pendingSummaryUpdate)
        {
            UpdateSummaryProperties();
            _pendingSummaryUpdate = false;
        }
    }

    private void UpdateComponentEditor()
    {
        if (ComponentKey == ComponentKey.Empty)
        {
            // Todo: This might happen temporarily when changing the component structure in an entity. Could just ignore it?
            _logger.LogError($"Failed to create component editor. Component key is not valid");
            _componentEditor = null;
            UpdateSummaryProperties();
            return;
        }

        // Always create a new component editor to ensure we're wrapping the correct component and not using stale data.
        _componentEditor = null;

        var createEditorResult = _entityService.CreateComponentEditor(ComponentKey);
        if (createEditorResult.IsFailure)
        {
            _logger.LogError($"Failed to create component editor for {ComponentKey}: {createEditorResult.Error}");
            UpdateSummaryProperties();
            return;
        }
        _componentEditor = createEditorResult.Value;
    }

    private void UpdateSummaryProperties()
    {
        if (_componentEditor == null)
        {
            SummaryText = string.Empty;
            return;
        }

        // Get the component summary from the component editor
        var getSummaryResult = _componentEditor.GetComponentSummary();
        if (getSummaryResult.IsFailure)
        {
            _logger.LogError($"Failed to get component summary for {ComponentKey}: {getSummaryResult.Error}");
            UpdateSummaryProperties();
            return;
        }
        var summary = getSummaryResult.Value;

        // Copy the summary details to the View Model properties to update the view
        SummaryText = summary.SummaryText;
    }
}
