using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.Models;

/// <summary>
/// Model class for a component item.
/// </summary>
public partial class ComponentItem : ObservableObject
{
    [ObservableProperty]
    private string _componentType = string.Empty;

    [ObservableProperty]
    private string _componentDescription = string.Empty;

    [ObservableProperty]
    private ComponentStatus _componentStatus;

    [ObservableProperty]
    private Visibility _showErrorIcon = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _showWarningIcon = Visibility.Collapsed;

    public ComponentItem()
    {
        PropertyChanged += ComponentItem_PropertyChanged;
    }

    private void ComponentItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComponentStatus))
        {
            switch (this.ComponentStatus)
            {
                case ComponentStatus.Valid:
                    ShowErrorIcon = Visibility.Collapsed;
                    ShowWarningIcon = Visibility.Collapsed;
                    break;

                case ComponentStatus.Error:
                    ShowErrorIcon = Visibility.Visible;
                    ShowWarningIcon = Visibility.Collapsed;
                    break;

                case ComponentStatus.Warning:
                    ShowErrorIcon = Visibility.Collapsed;
                    ShowWarningIcon = Visibility.Visible;
                    break;
            }
        }
    }

    public ComponentItem DeepClone()
    {
        return new ComponentItem
        {
            ComponentType = ComponentType,
            ComponentDescription = ComponentDescription
        };
    }
}
