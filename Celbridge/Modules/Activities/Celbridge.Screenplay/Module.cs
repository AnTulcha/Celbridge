using Celbridge.Activities;
using Celbridge.Modules;
using Celbridge.Screenplay;

namespace Celbridge.Workspace;

public class Module : IModule
{
    private const string ActivityName = "Screenplay";

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

    public IList<string> SupportedActivities { get; } = new List<string>() { ActivityName };

    public Result<IActivity> CreateActivity(string activityName)
    {
        var serviceProvider = ServiceLocator.ServiceProvider;

        if (activityName == "Screenplay")
        {
            var activity = serviceProvider.GetRequiredService<ScreenplayActivity>();
            return Result<IActivity>.Ok(activity);
        }

        return Result<IActivity>.Fail();
    }
}
