using System.ComponentModel;
using Celbridge.Entities;
using Celbridge.Inspector.ViewModels;
using Celbridge.Logging;

namespace Celbridge.Inspector.Views;

public sealed partial class ComponentView : UserControl, INotifyPropertyChanged
{
    private ILogger<ComponentView> _logger;

    public ComponentViewModel ViewModel { get; }

    public ComponentKey _componentKey;
    public ComponentKey ComponentKey
    {
        get => _componentKey;
        set
        {
            if (_componentKey != value)
            {
                // _logger.LogInformation($"ComponentKey changed from {_componentKey} to {value}");
                _componentKey = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ComponentKey)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ComponentView()
    {
        this.InitializeComponent();

        _logger = ServiceLocator.AcquireService<ILogger<ComponentView>>();

        ViewModel = ServiceLocator.AcquireService<ComponentViewModel>();

        PropertyChanged += ComponentView_PropertyChanged;

        Unloaded += (s,e) => ViewModel.OnViewUnloaded();
    }

    private void ComponentView_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComponentKey))
        {
            // Update the component key in the view model
            ViewModel.ComponentKey = ComponentKey;
        }
    }
}
