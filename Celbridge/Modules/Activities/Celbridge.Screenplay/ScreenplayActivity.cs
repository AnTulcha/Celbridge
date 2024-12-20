using System.Text;
using Celbridge.Activities;
using Celbridge.Documents;
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
    private readonly IDocumentsService _documentService;

    public string ActivityName => "Screenplay";

    public ScreenplayActivity(
        ILogger<ScreenplayActivity> logger,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _documentService = workspaceWrapper.WorkspaceService.DocumentsService;
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

        bool hasScreenplayComponents = false;

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

            hasScreenplayComponents = true;

            Result<ComponentAppearance> getAppearanceResult = componentInfo.ComponentType switch
            {
                "Scene" => GetSceneAppearance(resource, i, componentInfo),
                "Line" => GetLineAppearance(resource, i, componentInfo),
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

        if (hasScreenplayComponents)
        {
            // Todo: Cache the generated markdown so that we only regenerate it when the component properties change

            var generateResult = GenerateScreenplayMarkdown(resource);
            if (generateResult.IsFailure)
            {
                return Result.Fail($"Failed to generate screenplay markdown");
            }

            var markdown = generateResult.Value;

            // Set the contents of the document to the generated markdown
            var setContentResult = _documentService.SetTextDocumentContent(resource, markdown);
            if (setContentResult.IsFailure)
            {
                return Result.Fail($"Failed to set document content")
                    .WithErrors(setContentResult);
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result<ComponentAppearance> GetSceneAppearance(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
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

    private Result<ComponentAppearance> GetLineAppearance(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
    {
        var getCharacterResult = _entityService.GetProperty<String>(resource, componentIndex, "/character");
        if (getCharacterResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getCharacterResult);
        }
        var character = getCharacterResult.Value;

        var getSourceTextResult = _entityService.GetProperty<String>(resource, componentIndex, "/sourceText");
        if (getSourceTextResult.IsFailure)
        {
            return Result<ComponentAppearance>.Fail().WithErrors(getSourceTextResult);
        }
        var sourceText = getSourceTextResult.Value;

        // Todo: Use a localized string to format this
        var description = $"{character}: {sourceText}";

        var componentAppearance = new ComponentAppearance(description);

        return Result<ComponentAppearance>.Ok(componentAppearance);
    }

    private Result<string> GenerateScreenplayMarkdown(ResourceKey resource)
    {
        var getSceneResult = _entityService.GetComponentsOfType(resource, "Scene");
        if (getSceneResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get Scene component")
                .WithErrors(getSceneResult);
        }
        var sceneIndices = getSceneResult.Value;

        if (sceneIndices.Count != 1)
        {
            return Result<string>.Fail($"Failed to get Scene component");
        }

        var sceneComponentIndex = sceneIndices[0];

        var getTitleResult = _entityService.GetProperty<string>(resource, sceneComponentIndex, "/sceneTitle");
        if (getTitleResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get scene title")
                .WithErrors(getTitleResult);
        }
        var sceneTitle = getTitleResult.Value;

        var getDescriptionResult = _entityService.GetProperty<string>(resource, sceneComponentIndex, "/sceneDescription");
        if (getDescriptionResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get scene description")
                .WithErrors(getDescriptionResult);
        }
        var sceneDescription = getDescriptionResult.Value;

        var sb = new StringBuilder();

        sb.AppendLine($"# {sceneTitle}");
        sb.AppendLine();
        sb.AppendLine($"{sceneDescription}");
        sb.AppendLine();

        var getLinesResult = _entityService.GetComponentsOfType(resource, "Line");
        if (getLinesResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get Line components")
                .WithErrors(getLinesResult);
        }
        var lineComponentIndices = getLinesResult.Value;

        foreach (var lineComponentIndex in lineComponentIndices)
        {
            var getCharacterResult = _entityService.GetProperty<string>(resource, lineComponentIndex, "/character");
            if (getCharacterResult.IsFailure)
            {
                return Result<string>.Fail($"Failed to get character")
                    .WithErrors(getCharacterResult);
            }
            var character = getCharacterResult.Value;

            var getSourceTextResult = _entityService.GetProperty<string>(resource, lineComponentIndex, "/sourceText");
            if (getSourceTextResult.IsFailure)
            {
                return Result<string>.Fail($"Failed to get source text")
                    .WithErrors(getSourceTextResult);
            }
            var sourceText = getSourceTextResult.Value;

            if (string.IsNullOrWhiteSpace(character) || string.IsNullOrWhiteSpace(sourceText))
            {
                continue;
            }

            sb.AppendLine($"**{character}**: {sourceText}");
            sb.AppendLine();
        }

        var markdown = sb.ToString();

        return Result<string>.Ok(markdown);
    }
}
