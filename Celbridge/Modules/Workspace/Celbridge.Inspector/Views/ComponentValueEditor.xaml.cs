using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentValueEditor : UserControl
{
    public ComponentValueEditorViewModel ViewModel { get; set; }

    public ComponentValueEditor()
    {
        this.InitializeComponent();

        var serviceProvider = ServiceLocator.ServiceProvider;
        ViewModel = serviceProvider.GetRequiredService<ComponentValueEditorViewModel>();

        DataContext = ViewModel;

        ViewModel.OnFormsCreated += ViewModel_OnFormsCreated;
    }

    private void ViewModel_OnFormsCreated(List<IForm> forms)
    {
        PropertyForms.Children.Clear();

        foreach (var form in forms)
        {
            var uiElement = form.FormUIElement as UIElement;
            PropertyForms.Children.Add(uiElement);
        }
    }
}
