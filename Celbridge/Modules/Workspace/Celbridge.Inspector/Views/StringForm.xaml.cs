using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public sealed partial class StringForm : UserControl
{
    public StringFormViewModel ViewModel { get; private set; }

    public StringForm()
    {
        this.InitializeComponent();

        var serviceProvider = ServiceLocator.ServiceProvider;
        ViewModel = serviceProvider.GetRequiredService<StringFormViewModel>();

        DataContext = ViewModel;

        Unloaded += (s, e) => ViewModel.OnViewUnloaded();
    }
}
