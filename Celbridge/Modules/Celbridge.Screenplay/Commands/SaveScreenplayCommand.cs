using Celbridge.Commands;
using Celbridge.Screenplay.Services;
using Celbridge.Workspace;

namespace Celbridge.Screenplay.Commands;

public class SaveScreenplayCommand : CommandBase
{
    private readonly IServiceProvider _serviceProvider;

    public ResourceKey ExcelFile { get; set; } = ResourceKey.Empty;

    public SaveScreenplayCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var workspaceWrapper = _serviceProvider.AcquireService<IWorkspaceWrapper>();
        var activityService = workspaceWrapper.WorkspaceService.ActivityService;

        var getActivityResult = activityService.GetActivity(nameof(ScreenplayActivity));
        if (getActivityResult.IsFailure)
        {
            return Result.Fail($"Failed to get activity")
                .WithErrors(getActivityResult);
        }

        var screenplayActivity = getActivityResult.Value as ScreenplayActivity;
        if (screenplayActivity is null)
        {
            return Result.Fail($"Activity is not a screenplay activity")
                .WithErrors(getActivityResult);
        }

        var saveResult = await screenplayActivity.SaveScreenplayAsync(ExcelFile);
        if (saveResult.IsFailure)
        {
            return Result.Fail($"Failed to save screenplay to Workbook")
                .WithErrors(saveResult);
        }

        return Result.Ok();
    }
}
