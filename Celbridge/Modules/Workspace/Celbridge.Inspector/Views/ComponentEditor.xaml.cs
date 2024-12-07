using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentEditor : UserControl
{
    public ComponentEditorViewModel ViewModel { get; set; }

    public ComponentEditor()
	{
		this.InitializeComponent();

        var serviceProvider = ServiceLocator.ServiceProvider;
        ViewModel = serviceProvider.GetRequiredService<ComponentEditorViewModel>();

        DataContext = ViewModel;
    }
}
