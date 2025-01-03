using Celbridge.Activities;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Inspector;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using System.Text;

namespace Celbridge.Screenplay.Services;

public class ScreenplayActivity : IActivity
{
    private readonly ILogger<ScreenplayActivity> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;
    private readonly IDocumentsService _documentService;

    private HashSet<ResourceKey> _pendingEntityUpdates = new();

    public ScreenplayActivity(
        ILogger<ScreenplayActivity> logger,        
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _documentService = workspaceWrapper.WorkspaceService.DocumentsService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;
    }

    public async Task<Result> Start()
    {
        _messengerService.Register<SelectedResourceChangedMessage>(this, (s,e) => HandleMessage(e.Resource));
        _messengerService.Register<ComponentChangedMessage>(this, (s, e) => HandleMessage(e.Resource));
        _messengerService.Register<PopulatedComponentListMessage>(this, (s, e) => HandleMessage(e.Resource));

        void HandleMessage(ResourceKey resource)
        {
            bool hasScreenplayTag = _entityService.HasTag(resource, ScreenplayConstants.ScreenplayTag);

            if (hasScreenplayTag)
            {
                _pendingEntityUpdates.Add(resource);
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    public async Task<Result> Stop()
    {
        _messengerService.Unregister<SelectedResourceChangedMessage>(this);
        _messengerService.Unregister<ComponentChangedMessage>(this);
        _messengerService.Unregister<PopulatedComponentListMessage>(this);

        await Task.CompletedTask;

        return Result.Ok();
    }

    public async Task<Result> UpdateAsync()
    {
        bool hasError = true;
        foreach (var fileResource in _pendingEntityUpdates)
        {
            bool hasScreenplayTag = _entityService.HasTag(fileResource, ScreenplayConstants.ScreenplayTag);
            if (!hasScreenplayTag)
            {
                // The Screenplay tag has been removed since the entity update was requested
                continue;
            }

            var updateResult = await UpdateEntity(fileResource);
            if (updateResult.IsFailure)
            {
                _logger.LogError(updateResult.Error);
                continue;
            }
        }
        _pendingEntityUpdates.Clear();

        if (hasError)
        {
            return Result.Fail("Failed to update a modified entity");
        }

        return Result.Ok();
    }

    public async Task<Result> UpdateEntity(ResourceKey fileResource)
    {
        var getCountResult = _entityService.GetComponentCount(fileResource);
        if (getCountResult.IsFailure)
        {
            // Inspected resource may have been deleted or moved since the update was requested
            return Result.Ok();
        }
        var componentCount = getCountResult.Value;

        // Populate the annotation data for each component associated with this activity

        bool hasScreenplayComponents = false;

        for (int i = 0; i < componentCount; i++)
        {
            // Check if the component's activityName property matches this activity's name

            var getInfoResult = _entityService.GetComponentTypeInfo(fileResource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail(fileResource, $"Failed to get component info for component index '{i}' on inspected resource: '{fileResource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            if (!componentInfo.HasTag(ScreenplayConstants.ScreenplayTag))
            {
                // Not a Screenplay component, so ignore it
                continue;
            }

            hasScreenplayComponents = true;

            Result<ComponentAnnotation> getAnnotationResult = componentInfo.ComponentType switch
            {
                ScreenplayConstants.SceneComponentType => GetSceneAnnotation(fileResource, i, componentInfo),
                ScreenplayConstants.LineComponentType => GetLineAnnotation(fileResource, i, componentInfo),
                _ => Result<ComponentAnnotation>.Fail($"{nameof(ScreenplayActivity)} does not support component type '{componentInfo.ComponentType}'")
            };

            if (getAnnotationResult.IsFailure)
            {
                // Todo: Display a user-facing error messsage via the annotation data instead of returning here
                _logger.LogError(getAnnotationResult.Error);
                continue;
            }
            var annotation = getAnnotationResult.Value;

            var updateAnnotationResult = _entityService.UpdateComponentAnnotation(fileResource, i, annotation);
            if (updateAnnotationResult.IsFailure)
            {
                return Result.Fail($"Failed to update annotation for component index '{i}' on inspected resource: '{fileResource}'")
                    .WithErrors(updateAnnotationResult);
            }
        }

        if (hasScreenplayComponents)
        {
            var generateResult = GenerateScreenplayMarkdown(fileResource);
            if (generateResult.IsFailure)
            {
                return Result.Fail($"Failed to generate screenplay markdown").
                    WithErrors(generateResult);
            }

            var markdown = generateResult.Value;

            // Set the contents of the document to the generated markdown
            var setContentResult = _documentService.SetTextDocumentContent(fileResource, markdown);
            if (setContentResult.IsFailure)
            {
                return Result.Fail($"Failed to set document content")
                    .WithErrors(setContentResult);
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result<ComponentAnnotation> GetSceneAnnotation(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
    {
        var getTitleResult = _entityService.GetProperty<string>(resource, componentIndex, ScreenplayConstants.SceneComponentProperty_SceneTitle);
        if (getTitleResult.IsFailure)
        {
            return Result<ComponentAnnotation>.Fail().WithErrors(getTitleResult);
        }
        var sceneTitle = getTitleResult.Value;

        var getDescriptionResult = _entityService.GetProperty<string>(resource, componentIndex, ScreenplayConstants.SceneComponentProperty_SceneDescription);
        if (getDescriptionResult.IsFailure)
        {
            return Result<ComponentAnnotation>.Fail().WithErrors(getDescriptionResult);
        }
        var sceneDescription = getDescriptionResult.Value;

        // Todo: Use a localized string to format this
        var componentDescription = $"{sceneTitle}: {sceneDescription}";

        var annotation = new ComponentAnnotation(ComponentStatus.Valid, componentDescription);

        return Result<ComponentAnnotation>.Ok(annotation);
    }

    private Result<ComponentAnnotation> GetLineAnnotation(ResourceKey resource, int componentIndex, ComponentTypeInfo componentInfo)
    {
        var getCharacterResult = _entityService.GetProperty<string>(resource, componentIndex, ScreenplayConstants.LineComponentProperty_Character);
        if (getCharacterResult.IsFailure)
        {
            return Result<ComponentAnnotation>.Fail().WithErrors(getCharacterResult);
        }
        var character = getCharacterResult.Value;

        var getSourceTextResult = _entityService.GetProperty<string>(resource, componentIndex, ScreenplayConstants.LineComponentProperty_SourceText);
        if (getSourceTextResult.IsFailure)
        {
            return Result<ComponentAnnotation>.Fail().WithErrors(getSourceTextResult);
        }
        var sourceText = getSourceTextResult.Value;

        // Todo: Use a localized string to format this
        var description = $"{character}: {sourceText}";

        var annotation = new ComponentAnnotation(ComponentStatus.Valid, description);

        return Result<ComponentAnnotation>.Ok(annotation);
    }

    private Result<string> GenerateScreenplayMarkdown(ResourceKey resource)
    {
        var getSceneResult = _entityService.GetComponentsOfType(resource, ScreenplayConstants.SceneComponentType);
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

        var getTitleResult = _entityService.GetProperty<string>(resource, sceneComponentIndex, ScreenplayConstants.SceneComponentProperty_SceneTitle);
        if (getTitleResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get scene title")
                .WithErrors(getTitleResult);
        }
        var sceneTitle = getTitleResult.Value;

        var getDescriptionResult = _entityService.GetProperty<string>(resource, sceneComponentIndex, ScreenplayConstants.SceneComponentProperty_SceneDescription);
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

        var getLinesResult = _entityService.GetComponentsOfType(resource, ScreenplayConstants.LineComponentType);
        if (getLinesResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get Line components")
                .WithErrors(getLinesResult);
        }
        var lineComponentIndices = getLinesResult.Value;

        foreach (var lineComponentIndex in lineComponentIndices)
        {
            var getCharacterResult = _entityService.GetProperty<string>(resource, lineComponentIndex, ScreenplayConstants.LineComponentProperty_Character);
            if (getCharacterResult.IsFailure)
            {
                return Result<string>.Fail($"Failed to get character")
                    .WithErrors(getCharacterResult);
            }
            var character = getCharacterResult.Value;

            var getSourceTextResult = _entityService.GetProperty<string>(resource, lineComponentIndex, ScreenplayConstants.LineComponentProperty_SourceText);
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
