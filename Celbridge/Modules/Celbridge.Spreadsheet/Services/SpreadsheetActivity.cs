using Celbridge.Activities;
using Celbridge.Commands;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Screenplay.Components;
using Celbridge.Workspace;

namespace Celbridge.Spreadsheet.Services;

public class SpreadsheetActivity : IActivity
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SpreadsheetActivity> _logger;
    private readonly ICommandService _commandService;
    private readonly IEntityService _entityService;
    private readonly IDocumentsService _documentService;

    public SpreadsheetActivity(
        IServiceProvider serviceProvider,
        ILogger<SpreadsheetActivity> logger,
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
        return extension == ".xlsx";
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

        // Add the Spreadsheet component
        var addResult = await _commandService.ExecuteImmediate<IAddComponentCommand>(command =>
        {
            command.ComponentKey = new ComponentKey(resource, 0);
            command.ComponentType = SpreadsheetEditor.ComponentType;
        });

        if (addResult.IsFailure)
        {
            return Result.Fail($"Failed to add component: {resource}")
                .WithErrors(addResult);
        }

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
        // Root component must be "Data.Spreadsheet"
        //

        var rootComponent = components[0];
        if (rootComponent.Schema.ComponentType == SpreadsheetEditor.ComponentType)
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
        // Todo: Remaining components must have tag "SpreadsheetComponent"
        //

        return Result.Ok();
    }

    public async Task<Result> UpdateResourceAsync(ResourceKey resource)
    {
        await Task.CompletedTask;

        return Result.Ok();
    }
}
