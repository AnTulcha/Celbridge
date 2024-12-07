using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public sealed partial class EntityEditor : UserControl
{
    public EntityEditorViewModel ViewModel { get; set; }

    public EntityEditor()
    {
        this.InitializeComponent();

        var serviceProvider = ServiceLocator.ServiceProvider;

        ViewModel = serviceProvider.GetRequiredService<EntityEditorViewModel>();
        DataContext = ViewModel;
    }

    public void ClearComponentsPanel()
    {
        ComponentListPanel.Children.Clear();
    }

    public void PopulateComponentsPanel(List<UIElement> elements)
    {
        ClearComponentsPanel();
        foreach (UIElement element in elements)
        {
            ComponentListPanel.Children.Add(element);
        }
    }

    public void ClearComponentInspector()
    {
        //ComponentInspector.PropertyInspectors.Children.Clear();
    }

    public void PopulateComponentInspector(List<UIElement> elements)
    {
        //ClearDetailElements();
        //foreach (UIElement element in elements)
        //{
        //    DetailPanel.Children.Add(element);
        //}
    }

    public void ClearComponentPicker()
    {
        //ComponentInspector.PropertyInspectors.Children.Clear();
    }

    public void PopulateComponentPicker(List<UIElement> elements)
    {
        //ClearDetailElements();
        //foreach (UIElement element in elements)
        //{
        //    DetailPanel.Children.Add(element);
        //}
    }
}
