﻿namespace Celbridge.Views.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public string Title => _stringLocalizer.GetString($"{nameof(SettingsPage)}.{nameof(Title)}");

    private IStringLocalizer _stringLocalizer;

    public SettingsPage()
    {
        var serviceProvider = ServiceLocator.ServiceProvider;
        _stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();

        ViewModel = serviceProvider.GetRequiredService<SettingsPageViewModel>();
        DataContext = ViewModel;

        this.DataContext<StartPageViewModel>((page, vm) => page
            .Background(Theme.Brushes.Background.Default)
            .Content(new Grid()
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
                .MinHeight(400)
                .Children(
                    new TextBlock()
                        .Text(Title)
            )));
    }
}



