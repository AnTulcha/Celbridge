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

        messengerService.Register<ComponentTypeInputTextChangedMessage>(this, OnComponentTypeInputTextChangedMessage);

        _entityService = workspaceWrapper.WorkspaceService.EntityService;
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

    static void SyncObservableCollection<T>(ObservableCollection<T> observable, IList<T> target)
    {
        int i = 0;

        // Traverse both lists and make changes as needed
        while (i < observable.Count && i < target.Count)
        {
            if (!EqualityComparer<T>.Default.Equals(observable[i], target[i]))
            {
                // Replace item if different
                observable[i] = target[i];
            }
            i++;
        }

        // Remove extra items from ObservableCollection
        while (observable.Count > target.Count)
        {
            observable.RemoveAt(observable.Count - 1);
        }

        // Add missing items from target list
        while (i < target.Count)
        {
            observable.Add(target[i]);
            i++;
        }
    }
}
