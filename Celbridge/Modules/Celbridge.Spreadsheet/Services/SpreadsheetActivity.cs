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
    private readonly ICommandService _commandService;
    private readonly IEntityService _entityService;

    public SpreadsheetActivity(
        ICommandService commandService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _commandService = commandService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
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

    public Result AnnotateEntity(ResourceKey entity, IEntityAnnotation entityAnnotation)
    {
        var getComponents = _entityService.GetComponents(entity);
        if (getComponents.IsFailure)
        {
            return Result.Fail(entity, $"Failed to get entity components: '{entity}'")
                .WithErrors(getComponents);
        }
        var components = getComponents.Value;

        if (components.Count != entityAnnotation.ComponentAnnotationCount)
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
            var error = new EntityReportItem(
                EntityReportType.Error,
                "Invalid root component",
                $"The root component must be a '{SpreadsheetEditor.ComponentType}'.");

            entityAnnotation.AddComponentError(0, error);
        }

        //
        // Remaining components must have attribute "isSpreadsheetComponent"
        //

        for (int i = 1; i < components.Count; i++)
        {
            var component = components[i];

            if (component.Schema.ComponentType == EntityConstants.EmptyComponentType)
            {
                // Skip empty components
                continue;
            }

            var isSpreadsheetComponent = component.Schema.GetBooleanAttribute("isSpreadsheetComponent");

            if (isSpreadsheetComponent)
            {
                entityAnnotation.SetIsRecognized(i);
            }
            else
            {
                var error = new EntityReportItem(
                    EntityReportType.Error,
                    "Invalid component type",
                    "This component is not compatible with the 'Data.Spreadsheet' component");

                entityAnnotation.AddComponentError(i, error);
            }
        }

        return Result.Ok();
    }

    public async Task<Result> UpdateResourceContentAsync(ResourceKey resource, IEntityAnnotation entityAnnotation)
    {
        await Task.CompletedTask;

        return Result.Ok();
    }
}
