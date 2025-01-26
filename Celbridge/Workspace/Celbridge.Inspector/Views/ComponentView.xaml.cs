using System.ComponentModel;
using Celbridge.Inspector.Models;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentView : UserControl, INotifyPropertyChanged
{
    public ComponentItem? _componentItem;
    public ComponentItem? ComponentItem
    {
        get => _componentItem;
        set
        {
            if (_componentItem != value)
            {
                _componentItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ComponentItem)));
            }
        }
    }


    public ComponentView()
    {
        this.InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
