using Celbridge.Modules;
using Celbridge.Inspector.Services;
using Celbridge.Inspector.ViewModels;
using Celbridge.Inspector.Views;

namespace Celbridge.Inspector;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<IInspectorService, InspectorService>();
        config.AddTransient<IInspectorFactory, InspectorFactory>();

        //
        // Register views
        //

        config.AddTransient<IInspectorPanel, InspectorPanel>();

        //
        // Register view models
        //

        config.AddTransient<InspectorPanelViewModel>();
        config.AddTransient<ResourceNameInspectorViewModel>();
        config.AddTransient<WebInspectorViewModel>();
        config.AddTransient<MarkdownInspectorViewModel>();
        config.AddTransient<ComponentListViewModel>();
        config.AddTransient<EntityEditorViewModel>();
        config.AddTransient<ComponentEditorViewModel>();
    }

    public static Result Initialize()
    {
        return Result.Ok();
    }
}
