﻿using Celbridge.BaseLibrary.UserInterface;
using Celbridge.CommonViews.Pages;
using Celbridge.CommonViews.UserControls;

namespace Celbridge.CommonViews;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<TitleBar>();
    }

    public static void Initialize()
    {
        var navigationService = Services.ServiceProvider.GetRequiredService<INavigationService>();

        navigationService.RegisterPage(nameof(StartPage), typeof(StartPage));
        navigationService.RegisterPage(nameof(SettingsPage), typeof(SettingsPage));
        navigationService.RegisterPage(nameof(NewProjectPage), typeof(NewProjectPage));
    }
}