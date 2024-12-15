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

        ViewModel.OnFromCreated += ViewModel_OnFormCreated;
    }

    private void ViewModel_OnFormCreated(List<IField> fields)
    {
        FormPanel.Children.Clear();

        foreach (var field in fields)
        {
            var uiElement = field.UIElement as UIElement;
            FormPanel.Children.Add(uiElement);
        }
    }
}
