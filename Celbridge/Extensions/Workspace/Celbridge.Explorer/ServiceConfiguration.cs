﻿using Celbridge.Commands.Services;
using Celbridge.Explorer.Commands;
using Celbridge.Explorer.Services;
using Celbridge.Explorer.ViewModels;
using Celbridge.Explorer.Views;
using Celbridge.Extensions;
using Celbridge.Validators;

namespace Celbridge.Explorer;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IExtensionServiceCollection config)
    {
        //
        // Register UI elements
        //
        config.AddTransient<ExplorerPanel>();

        //
        // Register View Models
        //
        config.AddTransient<ExplorerPanelViewModel>();
        config.AddTransient<ResourceTreeViewModel>();

        //
        // Register services
        //
        config.AddTransient<IExplorerService, ExplorerService>();
        config.AddTransient<IResourceRegistry, ResourceRegistry>();
        config.AddTransient<IResourceRegistryDumper, ResourceRegistryDumper>();
        config.AddTransient<ResourceArchiver>();

        //
        // Register commands
        //
        config.AddTransient<IUpdateResourcesCommand, UpdateResourcesCommand>();
        config.AddTransient<IAddResourceCommand, AddResourceCommand>();
        config.AddTransient<IDeleteResourceCommand, DeleteResourceCommand>();
        config.AddTransient<ICopyResourceCommand, CopyResourceCommand>();
        config.AddTransient<IAddResourceDialogCommand, AddResourceDialogCommand>();
        config.AddTransient<IDeleteResourceDialogCommand, DeleteResourceDialogCommand>();
        config.AddTransient<IRenameResourceDialogCommand, RenameResourceDialogCommand>();
        config.AddTransient<IDuplicateResourceDialogCommand, DuplicateResourceDialogCommand>();
        config.AddTransient<ISelectResourceCommand, SelectResourceCommand>();
        config.AddTransient<IExpandFolderCommand, ExpandFolderCommand>();

        //
        // Register validators
        //
        config.AddTransient<IResourceNameValidator, ResourceNameValidator>();
    }
}