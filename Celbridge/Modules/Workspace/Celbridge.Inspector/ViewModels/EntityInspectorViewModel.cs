using Celbridge.Entities;
using Celbridge.Inspector.Models;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Celbridge.Inspector.ViewModels;

public partial class EntityInspectorViewModel : InspectorViewModel
{
    private readonly ILogger<MarkdownInspectorViewModel> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    public ObservableCollection<ComponentItem> ComponentItems { get; } = new();

    [ObservableProperty]
    private int _selectedComponentIndex;

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
        PopulateComponentList();
    }

    public ICommand AddComponentCommand => new RelayCommand<int?>(AddComponent_Executed);
    private void AddComponent_Executed(int? index)
    {
        int addIndex;
        if (index is not null)
        {
            addIndex = (int)index;
        }
        else
        {
            // Select the index automatically
            if (SelectedComponentIndex == -1)
            {
                var countResult = _entityService.GetComponentCount(Resource);
                if (countResult.IsFailure)
                {
                    _logger.LogError(countResult.Error);
                    return;
                }

                addIndex = countResult.Value;
            }
            else
            {
                addIndex = SelectedComponentIndex + 1;
            }
        }


        var addComponentResult = _entityService.AddComponent(Resource, "Empty", addIndex);
        if (addComponentResult.IsFailure)
        {
            _logger.LogError(addComponentResult.Error);
            return;
        }

        PopulateComponentList();
    }

    public ICommand DeleteComponentCommand => new RelayCommand<int?>(DeleteComponent_Executed);
    private void DeleteComponent_Executed(int? index)
    {
        int deleteIndex;
        if (index is not null)
        {
            deleteIndex = (int)index;
        }
        else
        {
            // Select the index automatically
            deleteIndex = SelectedComponentIndex;
        }

        if (deleteIndex == -1)
        {
            return;
        }

        var deleteResult = _entityService.RemoveComponent(Resource, deleteIndex);
        if (deleteResult.IsFailure)
        {
            _logger.LogError(deleteResult.Error);
            return;
        }

        PopulateComponentList();
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        if (message.Resource == Resource &&
            message.PropertyPath == "/")
        {
            PopulateComponentList();
        }
    }

    private void EntityInspectorViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Resource))
        {
            // Todo: Update component list
        }
    }

    private void PopulateComponentList()
    {
        // Todo: Preserve selection if possible

        var getCountResult = _entityService.GetComponentCount(Resource);
        if (getCountResult.IsFailure)
        {
            _logger.LogError(getCountResult.Error);
            return;
        }
        var count = getCountResult.Value;

        int previousIndex = SelectedComponentIndex;

        List<ComponentItem> componentItems = new();
        for (int i = 0; i < count; i++)
        {
            var getComponentResult = _entityService.GetComponentInfo(Resource, i);
            if (getComponentResult.IsFailure)
            {
                _logger.LogError(getComponentResult.Error);
                return;
            }
            var componentInfo = getComponentResult.Value;

            var componentType = componentInfo.ComponentType;
            if (componentType == "Empty")
            {
                componentType = string.Empty;
            }

            var componentItem = new ComponentItem
            {
                ComponentType = componentType
            };

            componentItems.Add(componentItem);
        }

        ComponentItems.ReplaceWith(componentItems);

        SelectedComponentIndex = previousIndex;
    }
}
