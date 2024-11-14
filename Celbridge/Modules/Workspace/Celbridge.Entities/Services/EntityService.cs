using Celbridge.Entities.Models;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Projects;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;

using Path = System.IO.Path;

namespace Celbridge.Entities.Services;

public class EntityService : IEntityService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EntityService> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IProjectService _projectService;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    private readonly ConcurrentDictionary<ResourceKey, Entity> _entityCache = new(); // Cache for entity objects
    private readonly ConcurrentBag<ResourceKey> _modifiedEntities = new(); // Track modified entities

    private EntitySchemaService _schemaService;
    private EntityPrototypeService _prototypeService;

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
        _projectService = projectService;
        _workspaceWrapper = workspaceWrapper;

        _schemaService = serviceProvider.GetRequiredService<EntitySchemaService>();
        _prototypeService = serviceProvider.GetRequiredService<EntityPrototypeService>();

        _messengerService.Register<EntityPropertyChangedMessage>(this, OnEntityPropertyChangedMessage);
    }

    public async Task<Result> InitializeAsync()
    {
        var loadSchemasResult = await _schemaService.LoadSchemasAsync();
        if (loadSchemasResult.IsFailure)
        {
            return Result.Fail("Failed to load schemas")
                .WithErrors(loadSchemasResult);
        }

        var loadPrototypesResult = await _prototypeService.LoadPrototypesAsync(_schemaService);
        if (loadPrototypesResult.IsFailure)
        {
            return Result.Fail("Failed to load prototypes")
                .WithErrors(loadPrototypesResult);
        }

        var loadDefaultsResult = await _prototypeService.LoadFileEntityTypesAsync();
        if (loadDefaultsResult.IsFailure)
        {
            return Result.Fail("Failed to load default prototypes")
                .WithErrors(loadDefaultsResult);
        }

        return Result.Ok();
    }

    public Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource)
    {
        try
        {
            if (_entityCache.ContainsKey(oldResource))
            {
                var entity = _entityCache[oldResource];

                var newEntityPath = GetEntityDataPath(newResource);
                entity.SetResourceKey(newResource, newEntityPath);

                _entityCache[newResource] = entity;
                _entityCache.TryRemove(oldResource, out _);

                // Update the modified resources list
                if (_modifiedEntities.Contains(oldResource))
                {
                    _modifiedEntities.Add(newResource);
                    _modifiedEntities.TryTake(out oldResource);
                }

                // Rename the backing JSON file
                string oldEntityPath = GetEntityDataPath(oldResource);
                string newResourcePath = GetEntityDataPath(newResource);
                if (File.Exists(oldEntityPath))
                {
                    File.Move(oldEntityPath, newResourcePath);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remap entities for resource: '{oldResource}' to '{newResource}'")
                .WithException(ex);
        }
    }

    public async Task<Result> SaveModifiedEntities()
    {
        foreach (var resourceKey in _modifiedEntities)
        {
            if (_entityCache.ContainsKey(resourceKey))
            {
                var entity = _entityCache[resourceKey];

                var saveResult = await entity.SaveAsync();
                if (saveResult.IsFailure)
                {
                    return Result.Fail($"Failed to save entity data for resource: '{resourceKey}'")
                        .WithErrors(saveResult);
                }
            }
        }

        // Clear the modified entities list
        _modifiedEntities.Clear();

        return Result.Ok();
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyPath, T? defaultValue) where T : notnull
    {
        var acquireResult = AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return defaultValue;
        }
        var entity = acquireResult.Value as Entity;
        Guard.IsNotNull(entity);

        return entity.GetProperty(propertyPath, defaultValue);
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyPath) where T : notnull
    {
        return GetProperty(resource, propertyPath, default(T));
    }

    public void SetProperty<T>(ResourceKey resource, string propertyPath, T newValue) where T : notnull
    {
        var acquireResult = AcquireEntity(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return;
        }
        var entity = acquireResult.Value as Entity;
        Guard.IsNotNull(entity);

        if (entity.SetProperty(propertyPath, newValue))
        {
            _modifiedEntities.Add(resource);
        }
    }

    private void OnEntityPropertyChangedMessage(object recipient, EntityPropertyChangedMessage message)
    {
        var (resource, _, _) = message;

        _modifiedEntities.Add(resource);
    }

    private Result<Entity> AcquireEntity(ResourceKey resource)
    {
        if (_entityCache.ContainsKey(resource))
        {
            var entity = _entityCache[resource];
            return Result<Entity>.Ok(entity);
        }

        try
        {
            EntityData? entityData = null;

            string entityDataPath = GetEntityDataPath(resource);
            if (File.Exists(entityDataPath))
            {
                // Load the EntityData json
                var jsonObject = JsonNode.Parse(File.ReadAllText(entityDataPath)) as JsonObject;
                if (jsonObject is null)
                {
                    // Log an error and fall through to create a new entity
                    _logger.LogError($"Failed to parse entity data for resource: {resource}");
                }
                else
                {
                    // Get the entity type from the JSON object
                    if (jsonObject.TryGetPropertyValue("_entityType", out var entityTypeValue) &&
                        entityTypeValue?.GetValueKind() == System.Text.Json.JsonValueKind.String)
                    {
                        // Check if this is a valid entity type
                        var entityType = entityTypeValue.ToString();
                        if (!string.IsNullOrEmpty(entityType))
                        {
                            // Get the schema for the entity type
                            var getSchemaResult = _schemaService.GetSchemaByEntityType(entityType);
                            if (getSchemaResult.IsSuccess)
                            {
                                var entitySchema = getSchemaResult.Value;

                                // Validate the data against the schema
                                var validateResult = entitySchema.ValidateJsonObject(jsonObject);
                                if (validateResult.IsFailure)
                                {
                                    // Log an error and fall through to create a new entity
                                    _logger.LogError($"Entity data failed schema validation: {resource}");
                                }
                                else
                                {
                                    // We've passed validation so now we can create the EntityData object
                                    entityData = EntityData.Create(jsonObject, entitySchema);
                                }
                            }
                        }
                    }
                }
            }

            // We were unable to load an existing EntityData object, so we need to create a new one
            if (entityData is null)
            {
                EntityData? entityPrototype = null;

                // Attempt to find an entity type for the resource's file extension
                var fileExtension = Path.GetExtension(resource.ToString());
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    var entityTypes = _prototypeService.GetFileEntityTypes(fileExtension);
                    if (entityTypes.Count > 0)
                    {
                        // Default entity type is the first in the list
                        var entityTypeFromConfig = entityTypes[0];

                        // Get the prototype for the entity type
                        var getPrototypeResult = _prototypeService.GetPrototype(entityTypeFromConfig);
                        if (getPrototypeResult.IsFailure)
                        {
                            return Result<Entity>.Fail($"Failed to get prototype for entity type: {entityTypeFromConfig}")
                                .WithErrors(getPrototypeResult);
                        }

                        entityPrototype = getPrototypeResult.Value;
                    }
                }

                if (entityPrototype == null)
                {
                    // This resource does not have a registered entity type, so we need to assign a default entity
                    // type based on the resource type (file or folder).

                    var resourceRegistry = _workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;
                    var path = resourceRegistry.GetResourcePath(resource);

                    if (Directory.Exists(path))
                    {
                        // Folder resource
                        var getPrototypeResult = _prototypeService.GetPrototype("Folder");
                        if (getPrototypeResult.IsFailure)
                        {
                            return Result<Entity>.Fail($"Failed to get prototype for folder entity type")
                                .WithErrors(getPrototypeResult);
                        }

                        entityPrototype = getPrototypeResult.Value;
                    }
                    else if (File.Exists(path))
                    {
                        // File resource
                        var getPrototypeResult = _prototypeService.GetPrototype("File");
                        if (getPrototypeResult.IsFailure)
                        {
                            return Result<Entity>.Fail($"Failed to get prototype for file entity type")
                                .WithErrors(getPrototypeResult);
                        }

                        entityPrototype = getPrototypeResult.Value;
                    }
                }

                if (entityPrototype is null)
                {
                    // We should always have a prototype at this point
                    return Result<Entity>.Fail($"Failed to get entity prototype for resource: {resource}");
                }

                entityData = entityPrototype.DeepClone();
            }

            if (entityData is null)
            {
                // We should always have an EntityData at this point
                return Result<Entity>.Fail($"Failed to get entity prototype for resource: {resource}");
            }

            // Create the entity and add it to the cache
            var entity = _serviceProvider.GetRequiredService<Entity>();
            entity.SetEntityData(entityData);
            entity.SetResourceKey(resource, entityDataPath);

            _entityCache[resource] = entity;

            _modifiedEntities.Add(resource);

            return Result<Entity>.Ok(entity);
        }
        catch (Exception ex)
        {
            return Result<Entity>.Fail($"An exception occurred when loading entity data for resource: '{resource}'")
                .WithException(ex);
        }
    }

    private string GetEntityDataPath(ResourceKey resourceKey)
    {
        var projectDataFolderPath = _projectService.CurrentProject!.ProjectDataFolderPath;
        var path = Path.Combine(projectDataFolderPath, "Entities", $"{resourceKey}.json");

        return Path.GetFullPath(path);
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
