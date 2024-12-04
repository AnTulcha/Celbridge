using Celbridge.Inspector.Models;
using Celbridge.Inspector.ViewModels;

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

    private void OnAddComponentClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is ComponentItem component)
        {
            // Insert at the next index in the list
            int index = ViewModel.ComponentItems.IndexOf(component);
            if (index != -1)
            {
                ViewModel.AddComponentCommand.Execute(index + 1);
            }
        }
    }

    private void OnDeleteComponentClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem &&
            menuItem.DataContext is ComponentItem component)
        {
            // Insert at the next index in the list
            int index = ViewModel.ComponentItems.IndexOf(component);
            if (index != -1)
            {
                ViewModel.DeleteComponentCommand.Execute(index);
            }
        }
    }
}
