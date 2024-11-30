using Celbridge.Core;
using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Projects;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Celbridge.Entities.Services;

/// <summary>
/// Describes the context in which a patch is applied.
/// </summary>
public enum ApplyPatchContext
{
    /// <summary>
    /// Modifying an entity.
    /// </summary>
    Modify,

    /// <summary>
    /// Undoing a previously applied modification.
    /// </summary>
    Undo,

    /// <summary>
    /// Redoing a previously undone modification.
    /// </summary>
    Redo
}

public class EntityService : IEntityService, IDisposable
{
    public const string EntityConfigFolder = "EntityConfig";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntityService> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    private ComponentSchemaRegistry _componentSchemaRegistry;
    private ComponentPrototypeRegistry _componentPrototypeRegistry;
    private EntityRegistry _entityRegistry;

    private readonly Dictionary<string, List<string>> _defaultComponents = new();
    private JsonSchema? _entitySchema;

    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        // Serialize enums as strings rather than numbers
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EntityService(
        IServiceProvider serviceProvider,
        ILogger<EntityService> logger,
        IMessengerService messengerService,
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messengerService = messengerService;
        _workspaceWrapper = workspaceWrapper;

        _componentSchemaRegistry = serviceProvider.GetRequiredService<ComponentSchemaRegistry>();
        _componentPrototypeRegistry = serviceProvider.GetRequiredService<ComponentPrototypeRegistry>();
        _entityRegistry = serviceProvider.GetRequiredService<EntityRegistry>();

        _messengerService.Register<ResourceRegistryUpdatedMessage>(this, OnResourceRegistryUpdatedMessage);
    }

    public async Task<Result> InitializeAsync()
    {
        try 
        {
            // Build and cache the entity schema
            var builder = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(
                    ("_entityVersion", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Integer)
                        .Const(1)
                    ),
                    ("_components", new JsonSchemaBuilder()
                        .Type(SchemaValueType.Array)
                    )
                )
                .Required("_entityVersion", "_components");

            _entitySchema = builder.Build();
            Guard.IsNotNull(_entitySchema);

            var loadSchemasResult = await _componentSchemaRegistry.LoadComponentSchemasAsync();
            if (loadSchemasResult.IsFailure)
            {
                return Result.Fail("Failed to load component schemas")
                    .WithErrors(loadSchemasResult);
            }

            var loadPrototypesResult = await _componentPrototypeRegistry.LoadComponentPrototypesAsync(_componentSchemaRegistry);
            if (loadPrototypesResult.IsFailure)
            {
                return Result.Fail("Failed to load component prototypes")
                    .WithErrors(loadPrototypesResult);
            }

            var loadDefaultsResult = await _entityRegistry.Initialize(_entitySchema, _componentPrototypeRegistry);
            if (loadDefaultsResult.IsFailure)
            {
                return Result.Fail("Failed to load file default components")
                    .WithErrors(loadDefaultsResult);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An excepption occurred when initializing the entity service")
                .WithException(ex);
        }
    }

    public string GetEntityDataPath(ResourceKey resource)
    {
        return _entityRegistry.GetEntityDataPath(resource);
    }

    public string GetEntityDataRelativePath(ResourceKey resource)
    {
        var relativePath = $"{FileNameConstants.ProjectDataFolder}/{FileNameConstants.EntitiesFolder}/{resource}.json";
        return relativePath;
    }

    public Result<PatchSummary> ApplyPatch(ResourceKey resource, string patch)
    {
        return ApplyPatch(resource, patch, ApplyPatchContext.Modify);
    }

    public Result<bool> UndoPatch(ResourceKey resource)
    {
        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            return Result<bool>.Fail($"Failed to acquire entity: {resource}")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        if (entity.UndoStack.Count == 0)
        {
            // Undo stack is empty. Succeed but return false to indicate that no changes were undone.
            return Result<bool>.Ok(false);
        }

        // Pop the next patch summary from the Undo stack and apply it to the entity
        var patchSummary = entity.UndoStack.Pop();
        var reversePatch = patchSummary.ReversePatch;
        Guard.IsNotNull(reversePatch);

        var applyResult = ApplyPatch(resource, reversePatch, ApplyPatchContext.Undo);
        if (applyResult.IsFailure)
        {
            return Result<bool>.Fail($"Failed to apply undo patch to resource: {resource}");
        }

        // Succeed and return true to indicate that a patch was undone.
        return Result<bool>.Ok(true);
    }

    public Result<bool> RedoPatch(ResourceKey resource)
    {
        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            return Result<bool>.Fail($"Failed to acquire entity: {resource}")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        if (entity.RedoStack.Count == 0)
        {
            // Redo stack is empty. Succeed but return false to indicate no changes were redone.
            return Result<bool>.Ok(false);
        }

        // Pop the next patch summary from the Redo stack and apply it to the entity
        var patchSummary = entity.RedoStack.Pop();
        var reversePatch = patchSummary.ReversePatch;
        Guard.IsNotNull(reversePatch);

        var applyResult = ApplyPatch(resource, reversePatch, ApplyPatchContext.Redo);
        if (applyResult.IsFailure)
        {
            return Result<bool>.Fail($"Failed to apply redo patch to resource: {resource}");
        }

        // Succeed and return true to indicate that a patch was undone.
        return Result<bool>.Ok(true);
    }

    public async Task<Result> SaveModifiedEntities()
    {
        return await _entityRegistry.SaveModifiedEntities();
    }

    public Result MoveEntityDataFile(ResourceKey oldResource, ResourceKey newResource)
    {
        return _entityRegistry.MoveEntityDataFile(oldResource, newResource);
    }

    public Result CopyEntityDataFile(ResourceKey sourceResource, ResourceKey destResource)
    {
        return _entityRegistry.CopyEntityDataFile(sourceResource, destResource);
    }

    private record AddComponentOperation(string op, string path, JsonObject value);
    public Result AddComponent(ResourceKey resource, string componentType, int componentIndex)
    {
        // Acquire the entity for the specified resource

        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            return Result.Fail($"Failed to acquire entity: {resource}")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        // Acquire the schema for the specified componentType

        var componentSchemaResult = _componentSchemaRegistry.GetSchemaForComponentType(componentType);
        if (componentSchemaResult.IsFailure)
        {
            return Result.Fail($"Failed to get schema for component type: {componentType}")
                .WithErrors(componentSchemaResult);
        }
        var componentSchema = componentSchemaResult.Value;

        // Create an instance of the prototype in the Entity Data

        var getPrototypeResult = _componentPrototypeRegistry.GetPrototype(componentType);
        if (getPrototypeResult.IsFailure)
        {
            return Result.Fail($"Failed to acquire component prototype for component type: {componentType}")
                .WithErrors(getPrototypeResult);
        }
        var prototype = getPrototypeResult.Value;

        var operation = new AddComponentOperation("add", $"/_components/{componentIndex}", prototype.JsonObject);
        var jsonPatch = JsonSerializer.Serialize(operation, SerializerOptions);
        jsonPatch = $"[{jsonPatch}]";

        var applyResult = ApplyPatch(resource, jsonPatch);
        if (applyResult.IsFailure)
        {
            return Result.Fail($"Failed to apply patch to add component to entity: {resource}")
                .WithErrors(applyResult);
        }

        return Result.Ok();
    }

    private record RemoveComponentOperation(string op, string path);
    public Result RemoveComponent(ResourceKey resource, int componentIndex)
    {
        // Acquire the entity for the specified resource

        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            return Result.Fail($"Failed to acquire entity: {resource}")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        // Apply a patch to remove the component at the specified index

        var operation = new RemoveComponentOperation("remove", $"/_components/{componentIndex}");
        var jsonPatch = JsonSerializer.Serialize(operation, SerializerOptions);
        jsonPatch = $"[{jsonPatch}]";

        var applyResult = ApplyPatch(resource, jsonPatch);
        if (applyResult.IsFailure)
        {
            return Result.Fail($"Failed to apply patch to remove entity component at index '{componentIndex}' for resource: {resource}")
                .WithErrors(applyResult);
        }

        return Result.Ok();
    }

    public T? GetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath, T? defaultValue) where T : notnull
    {
        var getResult = GetProperty<T>(resource, componentIndex, propertyPath);
        if (getResult.IsFailure)
        {
            return default;
        }

        return getResult.Value;
    }

    public Result<T> GetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath) where T : notnull
    {
        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return Result<T>.Fail($"Failed to acquire entity for resource '{resource}'")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        var componentPropertyPath = GetComponentPropertyPath(componentIndex, propertyPath);

        var getResult = entity.EntityData.GetProperty<T>(componentPropertyPath);
        if (getResult.IsFailure)
        {
            return Result<T>.Fail($"Failed to get entity property '{propertyPath}' for resource '{resource}'")
                .WithErrors(getResult);
        }

        return getResult;
    }

    public Result<string> GetPropertyAsJSON(ResourceKey resource, int componentIndex, string propertyPath)
    {
        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return Result<string>.Fail($"Failed to acquire entity for resource '{resource}'")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        var componentPropertyPath = GetComponentPropertyPath(componentIndex, propertyPath);

        var getResult = entity.EntityData.GetPropertyAsJSON(componentPropertyPath);
        if (getResult.IsFailure)
        {
            return Result<string>.Fail($"Failed to get entity property '{propertyPath}' for resource '{resource}'")
                .WithErrors(getResult);
        }

        return getResult;
    }

    private record SetPropertyOperation(string op, string path, object value);
    public Result<PatchSummary> SetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath, T newValue) where T : notnull
    {
        string componentPropertyPath = GetComponentPropertyPath(componentIndex, propertyPath);

        // Set the property by applying a JSON patch
        var operation = new SetPropertyOperation("add", componentPropertyPath, newValue);
        var jsonPatch = JsonSerializer.Serialize(operation, SerializerOptions);
        jsonPatch = $"[{jsonPatch}]";

        var applyResult = ApplyPatch(resource, jsonPatch);
        if (applyResult.IsFailure)
        {
            return Result<PatchSummary>.Fail($"Failed to apply entity patch for resource: {resource}");
        }
        var patchSummary = applyResult.Value;

        return Result<PatchSummary>.Ok(patchSummary);
    }

    private static string GetComponentPropertyPath(int componentIndex, string propertyPath)
    {
        return $"/_components/{componentIndex}{propertyPath}";
    }

    private void OnResourceRegistryUpdatedMessage(object recipient, ResourceRegistryUpdatedMessage message)
    {
        _entityRegistry.CleanupEntities();
    }

    private Result<PatchSummary> ApplyPatch(ResourceKey resource, string patch, ApplyPatchContext context)
    {
        var acquireResult = _entityRegistry.AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            return Result<PatchSummary>.Fail($"Failed to acquire entity: {resource}")
                .WithErrors(acquireResult);
        }
        var entity = acquireResult.Value;
        Guard.IsNotNull(entity);

        var applyResult = entity.EntityData.ApplyPatch(resource, patch);
        if (applyResult.IsFailure)
        {
            return Result<PatchSummary>.Fail($"Failed to apply patch to entity for resource: {resource}")
                .WithErrors(applyResult);
        }
        var patchSummary = applyResult.Value;

        if (patchSummary.ComponentChangeMessages.Count > 0)
        {
            _entityRegistry.MarkModifiedEntity(resource);

            // Add the patch summary to the requested stack to support undo/redo
            switch (context)
            {
                case ApplyPatchContext.Modify:
                    // Execute: Add patch summary to the Undo stack and clear the Redo stack.
                    entity.UndoStack.Push(patchSummary);
                    entity.RedoStack.Clear();
                    break;
                case ApplyPatchContext.Undo:
                    // Undo: Add patch summary to the Redo stack.
                    entity.RedoStack.Push(patchSummary);
                    break;
                case ApplyPatchContext.Redo:
                    // Undo: Add patch summary to the Undo stack.
                    entity.UndoStack.Push(patchSummary);
                    break;
            }

            // Notify listeners of the component changes resulting from applying the patch
            foreach (var message in patchSummary.ComponentChangeMessages)
            {
                _messengerService.Send(message);
            }
        }

        return Result<PatchSummary>.Ok(patchSummary);
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed objects here
            }

            _disposed = true;
        }
    }

    ~EntityService()
    {
        Dispose(false);
    }
}
