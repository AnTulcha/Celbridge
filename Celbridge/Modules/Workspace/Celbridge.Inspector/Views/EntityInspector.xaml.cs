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
}
