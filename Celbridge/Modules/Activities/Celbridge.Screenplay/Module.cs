using Celbridge.Activities;
using Celbridge.Modules;
using Celbridge.Screenplay.Services;

namespace Celbridge.Workspace;

public class Module : IModule
{
    private const string ScreenplayActivityName = "Screenplay";

    public void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<ScreenplayActivity>();
    }

    public Result Initialize()
    {
        return Result.Ok();
    }

    public bool SupportsActivity(string activityName) => activityName == ScreenplayActivityName;

    public Result<IActivity> CreateActivity(string activityName)
    {
        var serviceProvider = ServiceLocator.ServiceProvider;

        if (activityName == ScreenplayActivityName)
        {
            var activity = serviceProvider.GetRequiredService<ScreenplayActivity>();
            return Result<IActivity>.Ok(activity);
        }

        return Result<IActivity>.Fail();
    }
}
