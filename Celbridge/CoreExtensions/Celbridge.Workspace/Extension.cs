﻿using Celbridge.BaseLibrary.Extensions;
using Celbridge.BaseLibrary.UserInterface;
using Celbridge.Workspace.ViewModels;
using Celbridge.Workspace.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Celbridge.Workspace;

public class Extension : IExtension
{
    public void ConfigureServices(IExtensionServiceCollection config)
    {
        config.AddTransient<WorkspacePageViewModel>();
    }

    public Result Initialize()
    {
        var navigationService = Services.ServiceProvider.GetRequiredService<INavigationService>();

        navigationService.RegisterPage(nameof(WorkspacePage), typeof(WorkspacePage));


        return Result.Ok();
    }
}