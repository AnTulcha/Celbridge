using Celbridge.Activities;
using Celbridge.Commands;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Screenplay.Components;
using Celbridge.Workspace;
using System.Text;

using Path = System.IO.Path;

namespace Celbridge.Screenplay.Services;

public class ScreenplayActivity : IActivity
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScreenplayActivity> _logger;
    private readonly ICommandService _commandService;
    private readonly IEntityService _entityService;
    private readonly IDocumentsService _documentService;

    public ScreenplayActivity(
        IServiceProvider serviceProvider,
        ILogger<ScreenplayActivity> logger,        
        ICommandService commandService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _commandService = commandService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _documentService = workspaceWrapper.WorkspaceService.DocumentsService;
    }

    public async Task<Result> ActivateAsync()
    {
        await Task.CompletedTask;
        return Result.Ok();
    }

    public async Task<Result> DeactivateAsync()
    {
        await Task.CompletedTask;
        return Result.Ok();
    }

    public bool SupportsResource(ResourceKey resource)
    {
        var extension = Path.GetExtension(resource);
        return extension == ".scene";
    }

    public async Task<Result> InitializeResourceAsync(ResourceKey resource)
    {
        if (!SupportsResource(resource))
        {
            return Result.Fail($"This activity does not support this resource: {resource}");
        }

        var count = _entityService.GetComponentCount(resource);
        if (count > 0)
        {
            // Entity has already been initialized
            return Result.Ok();
        }

        _entityService.AddComponent(new ComponentKey(resource, 0), SceneEditor.ComponentType);

        await Task.CompletedTask;

        return Result.Ok();
    }

    public Result UpdateEntityAnnotation(ResourceKey entity, IEntityAnnotation entityAnnotation)
    {
        var getComponents = _entityService.GetComponents(entity);
        if (getComponents.IsFailure)
        {
            return Result.Fail(entity, $"Failed to get entity components: '{entity}'")
                .WithErrors(getComponents);
        }
        var components = getComponents.Value;

        if (components.Count != entityAnnotation.Count)
        {
            return Result.Fail(entity, $"Component count does not match annotation count: '{entity}'");
        }

        //
        // Root component must be "Scene"
        //

        var rootComponent = components[0];
        if (rootComponent.Schema.ComponentType == SceneEditor.ComponentType)
        {
            entityAnnotation.SetIsRecognized(0);
        }
        else
        {
            var error = new ComponentError(
                ComponentErrorSeverity.Critical,
                "Invalid component position",
                "This component must be the first component.");

            entityAnnotation.AddError(0, error);
        }

        //
        // Remaining components must be "Line"
        //

        string lineId = string.Empty;

        for (int i = 1; i < components.Count; i++)
        {
            var component = components[i];

            if (component.Schema.ComponentType == EntityConstants.EmptyComponentType)
            {
                // Skip empty components
                continue;
            }

            if (component.Schema.ComponentType != LineEditor.ComponentType)
            {
                var error = new ComponentError(
                    ComponentErrorSeverity.Critical,
                    "Invalid component type",
                    "This component must be a 'Line' component");

                entityAnnotation.AddError(i, error);

                continue;
            }

            entityAnnotation.SetIsRecognized(i);

            // Indent player variant lines
            var dialogueKey = component.GetString("/dialogueKey");
            var segments = dialogueKey.Split('-');

            if (segments.Length != 3)
            {
                var error = new ComponentError(
                    ComponentErrorSeverity.Critical,
                    "Invalid dialogue key",
                    "The dialogue key must consist of 3 hyphen separated segments");
                continue;
            }

            var currentLineId = segments[2];
            if (string.IsNullOrEmpty(currentLineId))
            {
                var error = new ComponentError(
                    ComponentErrorSeverity.Critical,
                    "Invalid line id",
                    "The line id segment of the dialogue key must not be empty");
                continue;
            }

            // Player variant lines
            // 1. Character is a player character
            // 2. The preceding lines are player characters or the Player character
            // In this case, ensure the line id matches the main character line
            // A player character line on its on displays an error message

            // An identical namespace and line id indicates a player variant line
            if (lineId == currentLineId)
            {
                // Indent the line
                entityAnnotation.SetIndent(i, 1);
            }
            else
            {
                lineId = currentLineId;
            }
        }

        return Result.Ok();
    }

    public async Task<Result> UpdateResourceAsync(ResourceKey resource)
    {
        var count = _entityService.GetComponentCount(resource);
        if (count == 0)
        {
            // Resource may have been deleted or moved since the update was requested
            return Result.Ok();
        }

        var generateResult = GenerateScreenplayMarkdown(resource);
        if (generateResult.IsFailure)
        {
            return Result.Fail($"Failed to generate screenplay markdown").
                WithErrors(generateResult);
        }

        var markdown = generateResult.Value;

        // Set the contents of the document to the generated markdown
        var setContentResult = await _documentService.SetTextDocumentContentAsync(resource, markdown);
        if (setContentResult.IsFailure)
        {
            return Result.Fail($"Failed to set document content")
                .WithErrors(setContentResult);
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    public async Task<Result> ImportScreenplayAsync(ResourceKey screenplayResource)
    {
        var importer = _serviceProvider.AcquireService<ScreenplayImporter>();

        var importResult = await importer.ImportScreenplayAsync(screenplayResource);
        if (importResult.IsFailure)
        {
            return Result.Fail($"Failed to import screenplay data from Excel")
                .WithErrors(importResult);
        }

        return Result.Ok();
    }

    private Result<string> GenerateScreenplayMarkdown(ResourceKey resource)
    {
        var getComponentResult = _entityService.GetComponentOfType(resource, SceneEditor.ComponentType);
        if (getComponentResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get Scene component")
                .WithErrors(getComponentResult);
        }
        var sceneComponent = getComponentResult.Value;

        var categoryText = sceneComponent.GetString(SceneEditor.Category);
        var namespaceText = sceneComponent.GetString(SceneEditor.Namespace);

        var sb = new StringBuilder();

        sb.AppendLine($"# {categoryText}");
        sb.AppendLine();
        sb.AppendLine($"{namespaceText}");
        sb.AppendLine();

        var getLinesResult = _entityService.GetComponentsOfType(resource, LineEditor.ComponentType);
        if (getLinesResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get Line components")
                .WithErrors(getLinesResult);
        }
        var lineComponents = getLinesResult.Value;

        foreach (var lineComponent in lineComponents)
        {
            var character = lineComponent.GetString(LineEditor.CharacterId);
            var sourceText = lineComponent.GetString(LineEditor.SourceText);

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
