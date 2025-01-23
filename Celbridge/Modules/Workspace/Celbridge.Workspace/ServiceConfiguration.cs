using Celbridge.DataTransfer;
using Celbridge.Navigation;
using Celbridge.Workspace.Commands;
using Celbridge.Workspace.Services;
using Celbridge.Workspace.ViewModels;
using Celbridge.Workspace.Views;

namespace Celbridge.Workspace;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        //
        // Configure workspace sub-projects
        //

        Activities.ServiceConfiguration.ConfigureServices(services);
        Console.ServiceConfiguration.ConfigureServices(services);
        Documents.ServiceConfiguration.ConfigureServices(services);
        Entities.ServiceConfiguration.ConfigureServices(services);
        Explorer.ServiceConfiguration.ConfigureServices(services);
        GenerativeAI.ServiceConfiguration.ConfigureServices(services);
        Inspector.ServiceConfiguration.ConfigureServices(services);
        Scripting.ServiceConfiguration.ConfigureServices(services);
        Status.ServiceConfiguration.ConfigureServices(services);

        //
        // Register services
        //

        services.AddTransient<IWorkspaceSettingsService, WorkspaceSettingsService>();
        services.AddTransient<IWorkspaceService, WorkspaceService>();
        services.AddTransient<IDataTransferService, DataTransferService>();
        services.AddTransient<WorkspaceLoader>();

        //
        // Register view models
        //

        services.AddTransient<WorkspacePageViewModel>();

        //
        // Register commands
        //

        services.AddTransient<ICopyTextToClipboardCommand, CopyTextToClipboardCommand>();
        services.AddTransient<ICopyResourceToClipboardCommand, CopyResourceToClipboardCommand>();
        services.AddTransient<IPasteResourceFromClipboardCommand, PasteResourceFromClipboardCommand>();
        services.AddTransient<IToggleFocusModeCommand, ToggleFocusModeCommand>();
        services.AddTransient<IAlertCommand, AlertCommand>();

    }

    public static void Initialize()
    {
        var navigationService = ServiceLocator.ServiceProvider.GetRequiredService<INavigationService>();
        navigationService.RegisterPage(nameof(WorkspacePage), typeof(WorkspacePage));
    }
}
