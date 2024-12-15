using Celbridge.Inspector.ViewModels;
using Windows.System;

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

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var options = new FindNextElementOptions 
            { 
                SearchRoot = ((UIElement)sender).XamlRoot!.Content 
            };

            FocusManager.TryMoveFocus(FocusNavigationDirection.Next, options);

            e.Handled = true;
        }
    }
}
