﻿namespace Celbridge.UserInterface.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public LocalizedString TitleString => _stringLocalizer.GetString($"SettingsPage_Title");

    private IStringLocalizer _stringLocalizer;

    public SettingsPage()
    {
        var serviceProvider = ServiceLocator.ServiceProvider;
        _stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();

        ViewModel = serviceProvider.GetRequiredService<SettingsPageViewModel>();

        this.DataContext(ViewModel, (page, vm) => page
            .Content(new Grid()
                .HorizontalAlignment(HorizontalAlignment.Center)
                .VerticalAlignment(VerticalAlignment.Center)
                .MinHeight(400)
                .Children(
                    new TextBlock()
                        .Text(TitleString)
            )));
    }
}



