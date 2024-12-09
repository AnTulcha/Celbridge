using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentTypeEditor : UserControl
{
    public ComponentTypeEditorViewModel ViewModel { get; private set; }

    public ComponentTypeEditor()
	{
		this.InitializeComponent();

        var serviceProvider = ServiceLocator.ServiceProvider;
        ViewModel = serviceProvider.GetRequiredService<ComponentTypeEditorViewModel>();

        DataContext = ViewModel;
    }
}
