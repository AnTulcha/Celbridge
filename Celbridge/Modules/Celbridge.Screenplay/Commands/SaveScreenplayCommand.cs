using Celbridge.Commands;
using Celbridge.Screenplay.Services;
using Celbridge.Workspace;

namespace Celbridge.Screenplay.Commands;

public class SaveScreenplayCommand : CommandBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceSettings _workspaceSettings;

    public ResourceKey WorkbookResource { get; set; } = ResourceKey.Empty;

    public SaveScreenplayCommand(
        IServiceProvider serviceProvider,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _workspaceSettings = workspaceWrapper.WorkspaceService.WorkspaceSettings;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var workspaceWrapper = _serviceProvider.AcquireService<IWorkspaceWrapper>();
        var activityService = workspaceWrapper.WorkspaceService.ActivityService;

        var getActivityResult = activityService.GetActivity(nameof(ScreenplayActivity));
        if (getActivityResult.IsFailure)
        {
            return Result.Fail($"Failed to get Screenplay activity")
                .WithErrors(getActivityResult);
        }

        var screenplayActivity = getActivityResult.Value as ScreenplayActivity;
        if (screenplayActivity is null)
        {
            return Result.Fail($"Activity is not a Screenplay activity")
                .WithErrors(getActivityResult);
        }

        var saveResult = await screenplayActivity.SaveScreenplayAsync(WorkbookResource);
        if (saveResult.IsFailure)
        {
            return Result.Fail($"Failed to save screenplay to workbook")
                .WithErrors(saveResult);
        }

        // All modified scenes have now been saved, so reset the modified scenes list
        await _workspaceSettings.DeletePropertyAsync(ScreenplayConstants.ModifiedScenesKey);

        return Result.Ok();
    }
}
