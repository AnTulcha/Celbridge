﻿using Celbridge.BaseLibrary.UserInterface;

namespace Celbridge.UserInterface.Views;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; private set; }

    public LocalizedString Home => _stringLocalizer.GetString($"{nameof(MainPage)}_{nameof(Home)}");
    public LocalizedString NewProject => _stringLocalizer.GetString($"{nameof(MainPage)}_{nameof(NewProject)}");
    public LocalizedString OpenProject => _stringLocalizer.GetString($"{nameof(MainPage)}_{nameof(OpenProject)}");
    public LocalizedString CloseProject => _stringLocalizer.GetString($"{nameof(MainPage)}_{nameof(CloseProject)}");
    public LocalizedString LegacyApp => _stringLocalizer.GetString($"{nameof(MainPage)}_{nameof(LegacyApp)}");

    private IStringLocalizer _stringLocalizer;
    private IUserInterfaceService _userInterfaceService;

    private Grid _layoutRoot;
    private NavigationView _mainNavigation;
    private Frame _contentFrame;

    public MainPage()
    {
        var serviceProvider = ServiceLocator.ServiceProvider;
        _stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();
        _userInterfaceService = serviceProvider.GetRequiredService<IUserInterfaceService>();

        ViewModel = serviceProvider.GetRequiredService<MainPageViewModel>();

        _contentFrame = new Frame()
            .Background(StaticResource.Get<Brush>("ApplicationBackgroundBrush"))
            .Name("ContentFrame");

        _mainNavigation = new NavigationView()
            .Name("MainNavigation")
            .Grid(row: 1)
            .Background(StaticResource.Get<Brush>("PanelBackgroundABrush"))
            .IsBackButtonVisible(NavigationViewBackButtonVisible.Collapsed)
            .PaneDisplayMode(NavigationViewPaneDisplayMode.LeftMinimal)
            .MenuItems(
                new NavigationViewItem()
                    .Icon(new SymbolIcon(Symbol.Home))
                    .Tag(MainPageViewModel.HomeTag)
                    .Content(Home),
                new NavigationViewItem()
                    .Icon(new SymbolIcon(Symbol.NewFolder))
                    .Tag(MainPageViewModel.NewProjectTag)
                    .Content(NewProject),
                new NavigationViewItem()
                    .Icon(new SymbolIcon(Symbol.OpenFile))
                    .Tag(MainPageViewModel.OpenProjectTag)
                    .Content(OpenProject),
                new NavigationViewItemSeparator(),
                new NavigationViewItem()
                    .Icon(new SymbolIcon(Symbol.Admin))
                    .Tag(MainPageViewModel.LegacyTag)
                    .Content(LegacyApp)
                )
                .Content(_contentFrame);

        _layoutRoot = new Grid()
            .Name("LayoutRoot")
            .RowDefinitions("Auto, *")
            .Children(_mainNavigation);

        this.DataContext(ViewModel, (page, vm) => page
            .Content(_layoutRoot));

        Loaded += OnMainPage_Loaded;
        Unloaded += OnMainPage_Unloaded;
    }

    private void OnMainPage_Loaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        // Setup the custom title bar (Windows only)
        var titleBar = new TitleBar();
        _layoutRoot.Children.Add(titleBar);

        var mainWindow = _userInterfaceService.MainWindow as Window;
        mainWindow!.ExtendsContentIntoTitleBar = true;
        mainWindow!.SetTitleBar(titleBar);
#endif

        ViewModel.OnNavigate += OnViewModel_Navigate;
        ViewModel.OnMainPage_Loaded();

        // Begin listening for user navigation events
        _mainNavigation.ItemInvoked += OnMainPage_NavigationViewItemInvoked;
    }

    private void OnMainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.OnMainPage_Unloaded();

        // Unregister all event handlers to avoid memory leaks

        ViewModel.OnNavigate -= OnViewModel_Navigate;

        _mainNavigation.ItemInvoked -= OnMainPage_NavigationViewItemInvoked;

        Loaded -= OnMainPage_Loaded;
        Unloaded -= OnMainPage_Unloaded;
    }

    private Result OnViewModel_Navigate(Type pageType, object parameter)
    {
        if (_contentFrame.Content != null &&
            _contentFrame.Content.GetType() == pageType)
        {
            // Already at the requested page, so just early out.
            return Result.Ok();
        }

        if (_contentFrame.Navigate(pageType, parameter))
        {
            return Result.Ok();
        }
        return Result.Fail($"Failed to navigate to page type {pageType}");
    }

    private void OnMainPage_NavigationViewItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ViewModel.SelectNavigationItem(MainPageViewModel.SettingsTag);
            return;
        }

        var item = args.InvokedItemContainer as NavigationViewItem;
        Guard.IsNotNull(item);

        var navigationItemTag = item.Tag;
        Guard.IsNotNull(navigationItemTag);

        var tag = navigationItemTag.ToString();
        Guard.IsNotNullOrEmpty(tag);

        ViewModel.SelectNavigationItem(tag);
    }

    public void Navigate(Type pageType, object parameter)
    {
        Guard.IsNotNull(_contentFrame);

        if (_contentFrame.Content is null ||
            _contentFrame.Content.GetType() != pageType)
        {
            _contentFrame.Navigate(pageType, parameter);
        }
    }
}