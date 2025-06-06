using Celbridge.Activities;
using Celbridge.Modules;
using Celbridge.Screenplay.Commands;
using Celbridge.Screenplay.Components;
using Celbridge.Screenplay.Services;

namespace Celbridge.Screenplay;

public class Module : IModule
{
    public IReadOnlyList<string> SupportedActivities { get; } = new List<string>()
    {
        nameof(ScreenplayActivity)
    };

    public void ConfigureServices(IModuleServiceCollection services)
    {
        //
        // Register services
        //

        services.AddTransient<ScreenplayActivity>();
        services.AddTransient<ScreenplayLoader>();
        services.AddTransient<ScreenplaySaver>();

        //
        // Register components
        //

        services.AddTransient<LineEditor>();
        services.AddTransient<SceneEditor>();
        services.AddTransient<ScreenplayDataEditor>();
        services.AddTransient<ScreenplayActivityEditor>();

        //
        // Register commands
        //

        services.AddTransient<LoadScreenplayCommand>();
        services.AddTransient<SaveScreenplayCommand>();
    }

    public Result Initialize()
    {
        return Result.Ok();
    }

    public Result<IActivity> CreateActivity(string activityName)
    {
        if (activityName == nameof(ScreenplayActivity))
        {
            var activity = ServiceLocator.AcquireService<ScreenplayActivity>();
            return Result<IActivity>.Ok(activity);
        }

        return Result<IActivity>.Fail();
    }
}
