﻿using Celbridge.Documents.ViewModels;

namespace Celbridge.Documents.Views;

public sealed partial class DocumentsPanel : UserControl
{
    private TabView _tabView;

    public DocumentsPanelViewModel ViewModel { get; }

    public DocumentsPanel()
    {
        var serviceProvider = ServiceLocator.ServiceProvider;

        ViewModel = serviceProvider.GetRequiredService<DocumentsPanelViewModel>();

        _tabView = new TabView()
            .IsAddTabButtonVisible(false)
            .TabWidthMode(TabViewWidthMode.SizeToContent)
            //.TabCloseRequested = "DocumentTabView_TabCloseRequested"
            .VerticalAlignment(VerticalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("PanelBackgroundABrush"));

        // Create a placeholder TabViewItem
        var tabViewItem = new TabViewItem
        {
            Header = "<Placeholder>",
            Content = new TextBlock { Text = "This is a placeholder tab item." }
        };

        // Add the TabViewItem to the TabView
        _tabView.TabItems.Add(tabViewItem);

        //
        // Set the data context and page content
        // 

        this.DataContext(ViewModel, (userControl, vm) => userControl
            .Content(_tabView));

        UpdateTabstripEnds();

        // Listen for property changes on the ViewModel
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += DocumentsPanel_Loaded;
        Unloaded += DocumentsPanel_Unloaded;
    }

    private void DocumentsPanel_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnViewLoaded();
    }

    private void DocumentsPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.OnViewUnloaded();

        Loaded -= DocumentsPanel_Loaded;
        Unloaded -= DocumentsPanel_Unloaded;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsLeftPanelVisible) ||
            e.PropertyName == nameof(ViewModel.IsRightPanelVisible))
        {
            UpdateTabstripEnds();
        }
    }

    private void UpdateTabstripEnds()
    {
        // When the left and right workspace panels are hidden, the panel visibility toggle buttons may overlap the
        // TabStrip at the top of the center panel. To fix this, we dynamically add an invisible TabStripHeader and
        // TabStripFooter which adjusts the position of the tabs so that they don't overlap the toggle buttons.

        if (ViewModel.IsLeftPanelVisible)
        {
            _tabView.TabStripHeader = null;
        }
        else
        {
            _tabView.TabStripHeader = new Grid()
                .Width(96);
        }

        if (ViewModel.IsRightPanelVisible)
        {
            _tabView.TabStripFooter = null;
        }
        else
        {
            _tabView.TabStripFooter = new Grid()
                .Width(48);
        }
    }
}