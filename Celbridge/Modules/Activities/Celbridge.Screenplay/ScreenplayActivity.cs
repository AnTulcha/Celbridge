using Celbridge.Activities;
using Celbridge.Logging;

namespace Celbridge.Screenplay;

public class ScreenplayActivity : IActivity
{
    private readonly ILogger<ScreenplayActivity> _logger;

    public string ActivityName => "Screenplay";

    public ScreenplayActivity(ILogger<ScreenplayActivity> logger)
    {
        _logger = logger;

        _logger.LogInformation("ScreenplayActivity created");
    }

    public async Task<Result> UpdateInspectedEntity()
    {
        _logger.LogInformation("ScreenplayActivity.UpdateInspectedEntity");

        // Todo: Iterate over every component in the inspected entity
        // If the component's 'activityName` property matches this activity's name, then populate the component's meta data.

        await Task.CompletedTask;

        return Result.Ok();
    }
}
