using Celbridge.Inspector.Models;
using Celbridge.Inspector.ViewModels;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.UI.Input;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.UI.Core;

namespace Celbridge.Inspector.Views;

public partial class ComponentListView : UserControl, IInspector
{
    private IStringLocalizer _stringLocalizer;

    public LocalizedString AddComponentTooltipString => _stringLocalizer.GetString("EntityInspector_AddComponentTooltip");
    public LocalizedString ContextMenuAddTooltipString => _stringLocalizer.GetString("EntityInspector_ContextMenu_Add");
    public LocalizedString ContextMenuDeleteTooltipString => _stringLocalizer.GetString("EntityInspector_ContextMenu_Delete");
    public LocalizedString ContextMenuDuplicateTooltipString => _stringLocalizer.GetString("EntityInspector_ContextMenu_Duplicate");

    public ComponentListViewModel ViewModel { get; private set; }

    // Code gen requires a parameterless constructor
    public ComponentListView()
    {
        throw new NotImplementedException();
    }

    public ComponentListView(ComponentListViewModel viewModel)
    {
        this.InitializeComponent();

        ViewModel = viewModel;
        DataContext = ViewModel;

        var serviceProvider = ServiceLocator.ServiceProvider;
        _stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();

        Loaded += EntityInspector_Loaded;

#if WINDOWS
        // Remove the distracting animations when items are added or removed from the list
        ComponentList.ItemContainerTransitions.Clear();
#endif
    }

    private void EntityInspector_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnViewLoaded();
    }

    public ResourceKey Resource
    {
        set => ViewModel.Resource = value;
        get => ViewModel.Resource;
    }

    private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key;

        // Delete selected component keyboard shortcut
        if (key == VirtualKey.Delete)
        {
            int deleteIndex = ComponentList.SelectedIndex;
            if (deleteIndex >= 0)
            {
                ViewModel.DeleteComponentCommand.Execute(deleteIndex);
            }
            e.Handled = true;
            return;
        }
    }

    private int _dragStartIndex = -1;

    private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var listView = sender as ListView;
        if (listView?.ItemsSource is ObservableCollection<ComponentItem> items)
        {
            var draggedItem = e.Items.FirstOrDefault();
            _dragStartIndex = items.IndexOf((ComponentItem)draggedItem!);
        }
    }

    private void ListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (sender.ItemsSource is ObservableCollection<ComponentItem> items)
        {
            var draggedItem = args.Items.FirstOrDefault();
            int dragEndIndex = items.IndexOf((ComponentItem)draggedItem!);

            ViewModel.MoveComponentCommand.Execute((_dragStartIndex, dragEndIndex));
        }

        _dragStartIndex = -1;
    }

    private void ComponentMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ComponentContextMenu.Placement = FlyoutPlacementMode.Bottom;
            ComponentContextMenu.ShowAt(button);
        }
    }

    // If the user switches between editing component types for two different components, the the focus event for the
    // second component is sent _before_ the lost focus event for the first component.
    // We use a counter to track the number of focussed text boxes at any time, if it's greater than zero then we are in
    // component type editing mode.
    private int _focusCount;

    private void ComponentItem_DisplayTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            var parentGrid = (Grid)textBlock.Parent;

            var textBox = parentGrid.Children.OfType<TextBox>().FirstOrDefault();
            if (textBox != null)
            {
                textBlock.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;

                textBox.Focus(FocusState.Programmatic);
            }

            var menuButton = parentGrid.Children.OfType<Button>().FirstOrDefault();
            if (menuButton != null)
            {
                menuButton.Visibility = Visibility.Collapsed;
            }

            _focusCount++;

            ViewModel.IsEditingComponentType = _focusCount > 0;
        }
    }

    private void ComponentItem_EditTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void ComponentItem_EditTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var parentGrid = (Grid)textBox.Parent;

            var textBlock = parentGrid.Children.OfType<TextBlock>().FirstOrDefault();
            if (textBlock != null)
            {
                textBox.Visibility = Visibility.Collapsed;
                textBlock.Visibility = Visibility.Visible;
            }

            var menuButton = parentGrid.Children.OfType<Button>().FirstOrDefault();
            if (menuButton != null)
            {
                menuButton.Visibility = Visibility.Visible;
            }

            _focusCount--;

            ViewModel.IsEditingComponentType = _focusCount > 0;
        }
    }

    private void ComponentItem_EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Shift + Enter adds a new component after the current component
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (e.Key == VirtualKey.Enter)
        {
            var componentItem = textBox.DataContext as ComponentItem;
            Guard.IsNotNull(componentItem);

            int componentIndex = ViewModel.ComponentItems.IndexOf(componentItem);

            var shiftDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            if (shiftDown)
            {
                ViewModel.AddComponentCommand.Execute(componentIndex);                
                e.Handled = true;
            }
            else
            {
                ViewModel.NotifyComponentTypeEntered();

                if (string.IsNullOrEmpty(textBox.Text))
                {
                    var item = ComponentList.Items[componentIndex];
                    //if (item is not null)
                    //{
                    //    item.Focus(FocusState.Programmatic);
                    //}
                }
            }
        }
    }

    private void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        Guard.IsNotNull(textBox);

        var text = textBox.Text;
        ViewModel.NotifyComponentTypeInputTextChanged(text);
    }
}
