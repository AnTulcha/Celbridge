using Celbridge.Inspector.ViewModels;

namespace Celbridge.Inspector.Views;

public partial class EntityInspector : UserControl, IInspector
{
    // Code gen requires a parameterless constructor
    public EntityInspector()
    {
        throw new NotImplementedException();
    }

    public EntityInspector(EntityInspectorViewModel viewModel)
    {
        this.InitializeComponent();
        DataContext = viewModel;
    }

    public ResourceKey Resource { get; set; }

    public void AddItem(ListViewItem listViewItem)
    {
        //ComponentList.Items.Add(listViewItem);
    }

    //private void ShowComponentMockupButton_Click(object sender, RoutedEventArgs e)
    //{
    //    var listViewItem = new ListViewItem()
    //        .Content
    //        (
    //            new Grid()
    //                .ColumnDefinitions("*, 2*, auto")
    //                .Children
    //                (
    //                    new TextBox()
    //                        .Grid(column: 0)
    //                        .VerticalAlignment(VerticalAlignment.Center)
    //                        .Text("VoiceLine"),
    //                    new TextBlock()
    //                        .Grid(column: 1)
    //                        .Margin(8, 0, 0, 0)
    //                        .VerticalAlignment(VerticalAlignment.Center)
    //                        .Text("Darth Vader: No, I am your father!"),
    //                    new SymbolIcon()
    //                        .Grid(column: 2)
    //                        .Symbol(Symbol.Play)
    //                        .ToolTipService(null, null, "Play using text to speech")
    //                )
    //        );

    //    Guard.IsNotNull(_componentListView);
    //    _componentListView.AddItem(listViewItem);
    //}
}
