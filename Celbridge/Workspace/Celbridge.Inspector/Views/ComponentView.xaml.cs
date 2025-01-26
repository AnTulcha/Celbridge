using System.ComponentModel;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentView : UserControl, INotifyPropertyChanged
{
    private string _componentType = string.Empty;

    public string ComponentType
    {
        get => _componentType; 
        set
        {
            if (_componentType != value)
            {
                _componentType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ComponentType)));
            }
        }
    }

    public ComponentView()
    {
        this.InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
