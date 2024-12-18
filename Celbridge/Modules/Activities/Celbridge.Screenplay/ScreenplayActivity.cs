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

    public async Task<Result> Start()
    {
        await Task.CompletedTask;

        return Result.Ok();
    }

    public async Task<Result> Stop()
    {
        await Task.CompletedTask;

        return Result.Ok();
    }

    public async Task<Result> UpdateAsync()
    {
        // Todo: Put this into an UpdateInspectedEntityAsyc

        // Get the inspected entity's list of components

        var resource = _inspectorService.InspectedResource;
        if (resource.IsEmpty)
        {
            // Inspected resource has been cleared since the update was requested
            return Result.Ok();
        }

        var getCountResult = _entityService.GetComponentCount(resource);
        if (getCountResult.IsFailure)
        {
            // Inspected resource may have been deleted or moved since the update was requested
            return Result.Ok();
        }
        var componentCount = getCountResult.Value;

        // Populate the component appearance for each component associated with this activity

        for (int i = 0; i < componentCount; i++)
        {
            // Check if the component's activityName property matches this activity's name

            var getInfoResult = _entityService.GetComponentTypeInfo(resource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail(resource, $"Failed to get component info for component index '{i}' on inspected resource: '{resource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            var activityName = componentInfo.GetStringAttribute("activityName");

            if (activityName != ActivityName)
            {
                // This component is not associated with this activity so we can ignore it
                continue;
            }

            Result<ComponentAppearance> getAppearanceResult = componentInfo.ComponentType switch
            {
                "Scene" => GetSceneComponentAppearance(resource, i, componentInfo),
                "VoiceLine" => GetVoiceLineAppearance(resource, i, componentInfo),
                _ => Result<ComponentAppearance>.Fail($"{nameof(ScreenplayActivity)} does not support component type '{componentInfo.ComponentType}'")
            };

            if (getAppearanceResult.IsFailure)
            {
                // Todo: Display an error messsage in the ComponentDisplayProperties
                _logger.LogError(getAppearanceResult.Error);
                continue;
            }
            var componentAppearance = getAppearanceResult.Value;

            var setAppearanceResult = _inspectorService.UpdateComponentAppearance(resource, i, componentAppearance);
            if (setAppearanceResult.IsFailure)
            {
                return Result.Fail($"Failed to set component appearance for component index '{i}' on inspected resource: '{resource}'")
                    .WithErrors(setAppearanceResult);
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result<ComponentAppearance> GetSceneComponentAppearance(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
    {
        var getTitleResult = _entityService.GetProperty<String>(resource, componentIndex, "/sceneTitle");
        if (getTitleResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getTitleResult);
        }
        var sceneTitle = getTitleResult.Value;

        var getDescriptionResult = _entityService.GetProperty<String>(resource, componentIndex, "/sceneDescription");
        if (getDescriptionResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getDescriptionResult);
        }
        var sceneDescription = getDescriptionResult.Value;

        // Todo: Use a localized string to format this
        var componentDescription = $"{sceneTitle}: {sceneDescription}";

        var componentAppearance = new ComponentAppearance(componentDescription);

        return Result<ComponentAppearance>.Ok(componentAppearance);
    }

    private Result<ComponentAppearance> GetVoiceLineAppearance(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
    {
        var getSpeakerResult = _entityService.GetProperty<String>(resource, componentIndex, "/speaker");
        if (getSpeakerResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getSpeakerResult);
        }
        var speaker = getSpeakerResult.Value;

        var getLineResult = _entityService.GetProperty<String>(resource, componentIndex, "/line");
        if (getLineResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getLineResult);
        }
        var line = getLineResult.Value;

        // Todo: Use a localized string to format this
        var componentDescription = $"{speaker}: {line}";

        var componentAppearance = new ComponentAppearance(componentDescription);

        return Result<ComponentAppearance>.Ok(componentAppearance);
    }
}
