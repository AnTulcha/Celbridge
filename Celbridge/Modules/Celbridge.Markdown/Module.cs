using Celbridge.Activities;
using Celbridge.Markdown.Components;
using Celbridge.Markdown.Services;
using Celbridge.Modules;

namespace Celbridge.Workspace;

public class Module : IModule
{
    public IReadOnlyList<string> SupportedActivities { get; } = new List<string>()
    {
        "Markdown"
    };

    public void ConfigureServices(IModuleServiceCollection services)
    {
        //
        // Register services
        //

        services.AddTransient<MarkdownActivity>();
        services.AddTransient<MarkdownPreviewProvider>();
        services.AddTransient<AsciiDocPreviewProvider>();

        //
        // Register component editors
        //

        services.AddTransient<MarkdownEditor>();
    }

    public Result Initialize()
    {
        return Result.Ok();
    }

    public Result<IActivity> CreateActivity(string activityName)
    {
        if (activityName == MarkdownActivity.ActivityName)
        {
            var serviceProvider = ServiceLocator.ServiceProvider;   
            var activity = serviceProvider.GetRequiredService<MarkdownActivity>();
            return Result<IActivity>.Ok(activity);
        }

        return Result<IActivity>.Fail();
    }
}
