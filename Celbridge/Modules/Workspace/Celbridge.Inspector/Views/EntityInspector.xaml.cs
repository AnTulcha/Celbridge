using Celbridge.Inspector.Models;
using Celbridge.Inspector.ViewModels;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace Celbridge.Inspector.Views;

public partial class EntityInspector : UserControl, IInspector
{
    public EntityInspectorViewModel ViewModel { get; private set; }

    // Code gen requires a parameterless constructor
    public EntityInspector()
    {
        throw new NotImplementedException();
    }

    public EntityInspector(EntityInspectorViewModel viewModel)
    {
        this.InitializeComponent();

        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += EntityInspector_Loaded;
    }

    private void EntityInspector_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnViewLoaded();
    }

    public ResourceKey Resource
    {
        set => ViewModel.Resource = value;
        get => ViewModel.Resource;
    }

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var shiftDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

        // Shift + Enter adds a new component after the current component
        if (e.Key == VirtualKey.Enter && shiftDown)
        {
            e.Handled = true;

            if (sender is TextBox textBox)
            {
                var componentItem = textBox.DataContext as ComponentItem;
                if (componentItem != null)
                {
                    int index = ViewModel.ComponentItems.IndexOf(componentItem);
                    ViewModel.AddComponentCommand.Execute(index);
                }
            }
        }
    }
}
