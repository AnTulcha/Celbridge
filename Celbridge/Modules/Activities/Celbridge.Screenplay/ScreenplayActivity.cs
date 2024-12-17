using Celbridge.Activities;
using Celbridge.Entities;
using Celbridge.Inspector;
using Celbridge.Logging;
using Celbridge.Workspace;

namespace Celbridge.Screenplay;

public class ScreenplayActivity : IActivity
{
    private readonly ILogger<ScreenplayActivity> _logger;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;

    public string ActivityName => "Screenplay";

    public ScreenplayActivity(
        ILogger<ScreenplayActivity> logger,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;
    }

    public async Task<Result> UpdateInspectedEntityAppearanceAsync()
    {
        // Get the inspected entity components

        var inspectedResource = _inspectorService.InspectedResource;

        if (inspectedResource.IsEmpty)
        {
            // Inspected resource has been cleared since the update was requested
            return Result.Ok();
        }

        var getCountResult = _entityService.GetComponentCount(inspectedResource);
        if (getCountResult.IsFailure)
        {
            // Inspected resource may have been deleted or moved since the update was requested
            return Result.Ok();
        }
        var componentCount = getCountResult.Value;

        for (int i = 0; i < componentCount; i++)
        {
            // Check if the component's activityName property matches this activity's name

            var getInfoResult = _entityService.GetComponentTypeInfo(inspectedResource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail(inspectedResource, $"Failed to get component info for component index '{i}' on inspected resource: '{inspectedResource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            var activityName = componentInfo.GetStringAttribute("activityName");

            if (activityName != ActivityName)
            {
                // This component is not associated with this activity
                continue;
            }

            if (componentInfo.ComponentType == "Scene")
            {
                UpdateSceneComponentAppearance(inspectedResource, i, componentInfo);
            }
            else if (componentInfo.ComponentType == "VoiceLine")
            {
                UpdateVoiceLineAppearance(inspectedResource, i, componentInfo);
            }
            else
            {
                // Todo: Display an error messsage in the ComponentDisplayProperties
                _logger.LogError($"Screenplay activity does not support component type '{componentInfo.ComponentType}'");
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result UpdateSceneComponentAppearance(ResourceKey inspectedResource, int i, ComponentTypeInfo componentInfo)
    {
        var getTitleResult = _entityService.GetProperty<String>(inspectedResource, i, "/sceneTitle");
        if (getTitleResult.IsFailure)
        {
            return Result.Fail().WithErrors(getTitleResult);
        }
        var sceneTitle = getTitleResult.Value;

        var getDescriptionResult = _entityService.GetProperty<String>(inspectedResource, i, "/sceneDescription");
        if (getDescriptionResult.IsFailure)
        {
            return Result.Fail().WithErrors(getDescriptionResult);
        }
        var sceneDescription = getTitleResult.Value;

        _logger.LogInformation($"Title: {sceneTitle}, Description: {sceneDescription}");

        return Result.Ok();
    }

    private Result UpdateVoiceLineAppearance(ResourceKey inspectedResource, int i, ComponentTypeInfo componentInfo)
    {
        var getSpeakerResult = _entityService.GetProperty<String>(inspectedResource, i, "/speaker");
        if (getSpeakerResult.IsFailure)
        {
            return Result.Fail().WithErrors(getSpeakerResult);
        }
        var speaker = getSpeakerResult.Value;

        var getLineResult = _entityService.GetProperty<String>(inspectedResource, i, "/line");
        if (getLineResult.IsFailure)
        {
            return Result.Fail().WithErrors(getLineResult);
        }
        var line = getLineResult.Value;

        _logger.LogInformation($"Speaker: {speaker}, Line: {line}");

        return Result.Ok();
    }
}
