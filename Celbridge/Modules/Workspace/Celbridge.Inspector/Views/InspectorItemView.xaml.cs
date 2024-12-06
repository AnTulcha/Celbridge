
namespace Celbridge.Inspector.Views;

public sealed partial class InspectorItemView : UserControl
{
    public InspectorItemView()
    {
        this.InitializeComponent();
    }

    public void ClearComponentsPanel()
    {
        ComponentsPanel.Children.Clear();
    }

    public void PopulateComponentsPanel(List<UIElement> elements)
    {
        ClearComponentsPanel();
        foreach (UIElement element in elements)
        {
            ComponentsPanel.Children.Add(element);
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
