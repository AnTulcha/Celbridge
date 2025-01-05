using Celbridge.Activities;
using Celbridge.Modules;
using Celbridge.Screenplay.Components;
using Celbridge.Screenplay.Services;

namespace Celbridge.Workspace;

public class Module : IModule
{
    public void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<ScreenplayActivity>();

        //
        // Register compoments
        //

        config.AddTransient<LineComponent>();
        config.AddTransient<SceneComponent>();
        config.AddTransient<ScreenplayActivityComponent>();

        // Todo: Move these to more appropriate modules
        config.AddTransient<EmptyComponent>();
        config.AddTransient<MarkdownComponent>();
    }

    public Result Initialize()
    {
        return Result.Ok();
    }

    public bool SupportsActivity(string activityName) => activityName == ScreenplayConstants.ScreenplayTag;

    public Result<IActivity> CreateActivity(string activityName)
    {
        var serviceProvider = ServiceLocator.ServiceProvider;

        if (activityName == ScreenplayConstants.ScreenplayTag)
        {
            var activity = serviceProvider.GetRequiredService<Screenplay.Services.ScreenplayActivity>();
            return Result<IActivity>.Ok(activity);
        }

        return Result<IActivity>.Fail();
    }
}
