using Celbridge.Activities;
using Celbridge.Logging;

namespace Celbridge.Screenplay;

public class ScreenplayActivity : IActivity
{
    private readonly ILogger<ScreenplayActivity> _logger;

    public ScreenplayActivity(ILogger<ScreenplayActivity> logger)
    {
        _logger = logger;

        _logger.LogInformation("ScreenplayActivity created");
    }
}
