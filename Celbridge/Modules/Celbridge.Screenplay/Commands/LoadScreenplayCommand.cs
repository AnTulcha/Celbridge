using Celbridge.Activities;
using Celbridge.Commands;
using Celbridge.Dialog;
using Celbridge.Explorer;
using Celbridge.Screenplay.Services;
using Celbridge.Workspace;
using System.Text;

namespace Celbridge.Screenplay.Commands;

public class LoadScreenplayCommand : CommandBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;
    private readonly IWorkspaceSettings _workspaceSettings;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IActivityService _activityService;

    public override CommandFlags CommandFlags => CommandFlags.UpdateResources;

    public ResourceKey WorkbookResource { get; set; } = ResourceKey.Empty;

    public LoadScreenplayCommand(
        IServiceProvider serviceProvider,
        IDialogService dialogService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _dialogService = dialogService;
        _workspaceSettings = workspaceWrapper.WorkspaceService.WorkspaceSettings;
        _resourceRegistry = workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;
        _activityService = workspaceWrapper.WorkspaceService.ActivityService;
    }

    public override async Task<Result> ExecuteAsync()
    {
        // Check if load will overwrite any modified scenes
        var confirmed = await ConfirmLoad();
        if (!confirmed)
        {
            return Result.Ok();            
        }

        var getActivityResult = _activityService.GetActivity(nameof(ScreenplayActivity));
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

        var loadResult = await screenplayActivity.LoadScreenplayAsync(WorkbookResource);
        if (loadResult.IsFailure)
        {
            return Result.Fail($"Failed to load screenplay from workbook")
                .WithErrors(loadResult);
        }

        // Reset list of modified scenes
        await _workspaceSettings.DeletePropertyAsync(ScreenplayConstants.ModifiedScenesKey);

        return Result.Ok();
    }

    private async Task<bool> ConfirmLoad()
    {
        // Get the list of modified scenes from the workspace settings
        var modifiedScenes = await _workspaceSettings.GetPropertyAsync<HashSet<string>>(ScreenplayConstants.ModifiedScenesKey);
        if (modifiedScenes is null ||
            modifiedScenes.Count == 0)
        {
            return true;
        }

        // Construct a sorted list containing the filename of each scene 
        var sceneResources = new List<string>();
        foreach (var sceneResource in modifiedScenes)
        {
            var getResourceResult = _resourceRegistry.GetResource(sceneResource);
            if (getResourceResult.IsFailure)
            {
                // Ignore this scene resource because it no longer exists (e.g. user deleted it)
                continue;
            }

            var sceneFilename = Path.GetFileNameWithoutExtension(sceneResource);
            if (!string.IsNullOrEmpty(sceneFilename))
            {
                sceneResources.Add(sceneFilename);
            }
        }
        sceneResources.Sort();

        if (sceneResources.Count == 0)
        {
            // No existing scene resources will be affected by the load, so it can proceed.
            return true;
        }

        var maxScenes = 5;
        var sb = new StringBuilder();
        for (int i = 0; i < sceneResources.Count; i++)
        {
            var scene = sceneResources[i];
            if (i > maxScenes)
            {
                sb.Append($"...");
                break;
            }
            sb.AppendLine(scene.ToString());
        }
        var sceneList = sb.ToString();

        // Ask the user to confirm that they want to overwrite the modified scenes
        var message = $"Loading will overwrite modified scene files:\n{sceneList}\n\n Do you wish to continue?";
        var confirmResult = await _dialogService.ShowConfirmationDialogAsync("Confirm Load", message);
        if (confirmResult.IsFailure)
        {
            return false;
        }
        var confirmed = confirmResult.Value;

        return confirmed;
    }
}
