using Celbridge.Entities.Models;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Projects;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using System.Collections.Concurrent;

using Path = System.IO.Path;

namespace Celbridge.Entities.Services;

public class EntityService : IEntityService, IDisposable
{
    private readonly ILogger<EntityService> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IProjectService _projectService;
    private readonly ConcurrentDictionary<ResourceKey, ResourceData> _resourceDataCache = new(); // Cache for ResourceData objects
    private readonly ConcurrentBag<ResourceKey> _modifiedResources = new(); // Track modified resources

    private EntitySchemaService _schemaService;
    private EntityPrototypeService _prototypeService;

    public EntityService(
        IServiceProvider serviceProvider,
        ILogger<EntityService> logger,
        IMessengerService messengerService,
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _messengerService = messengerService;
        _projectService = projectService;

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

    private Result<Entity> CreateEntity(string entityType)
    {
        var getResult = _prototypeService.GetPrototype(entityType);
        if (getResult.IsFailure)
        {
            return Result<Entity>.Fail($"Failed to get prototype for entity type: {entityType}")
                .WithErrors(getResult);
        }
        var prototype = getResult.Value;

        var entity = Entity.Create(prototype);

        return Result<Entity>.Ok(entity);
    }

    public Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource)
    {
        try
        {
            if (_resourceDataCache.ContainsKey(oldResource))
            {
                var resourceData = _resourceDataCache[oldResource];

                var newResourceDataPath = GetResourceDataPath(newResource);
                resourceData.SetResourceKey(newResource, newResourceDataPath);

                _resourceDataCache[newResource] = resourceData;
                _resourceDataCache.TryRemove(oldResource, out _);

                // Update the modified resources list
                if (_modifiedResources.Contains(oldResource))
                {
                    _modifiedResources.Add(newResource);
                    _modifiedResources.TryTake(out oldResource);
                }

                // Rename the backing JSON file
                string oldResourcePath = GetResourceDataPath(oldResource);
                string newResourcePath = GetResourceDataPath(newResource);
                if (File.Exists(oldResourcePath))
                {
                    File.Move(oldResourcePath, newResourcePath);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remap resource: '{oldResource}' to '{newResource}'")
                .WithException(ex);
        }
    }

    public async Task<Result> SavePendingAsync()
    {
        foreach (var resourceKey in _modifiedResources)
        {
            if (_resourceDataCache.ContainsKey(resourceKey))
            {
                var resourceData = _resourceDataCache[resourceKey];

                var saveResult = await resourceData.SaveAsync();
                if (saveResult.IsFailure)
                {
                    return Result.Fail($"Failed to save resource data: '{resourceKey}'")
                        .WithErrors(saveResult);
                }
            }
        }

        // Clear the modified resources list
        _modifiedResources.Clear();

        return Result.Ok();
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyPath, T? defaultValue) where T : notnull
    {
        var acquireResult = AcquireResourceData(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return defaultValue;
        }
        var resourceData = acquireResult.Value as ResourceData;
        Guard.IsNotNull(resourceData);

        return resourceData.GetProperty(propertyPath, defaultValue);
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyPath) where T : notnull
    {
        return GetProperty(resource, propertyPath, default(T));
    }

    public void SetProperty<T>(ResourceKey resource, string propertyPath, T newValue) where T : notnull
    {
        var acquireResult = AcquireResourceData(resource);
        if (acquireResult.IsFailure)
        {
            _logger.LogError(acquireResult.Error);
            return;
        }
        var resourceData = acquireResult.Value as ResourceData;
        Guard.IsNotNull(resourceData);

        resourceData.SetProperty(propertyPath, newValue);
    }

    private void OnEntityPropertyChangedMessage(object recipient, EntityPropertyChangedMessage message)
    {
        var (resource, _, _) = message;

        _modifiedResources.Add(resource);
    }

    private Result<ResourceData> AcquireResourceData(ResourceKey resource)
    {
        if (_resourceDataCache.ContainsKey(resource))
        {
            var resourceData = _resourceDataCache[resource];

            return Result<ResourceData>.Ok(resourceData);
        }

        try
        {
            // Create and load the ResourceData object
            var resourceData = new ResourceData(_messengerService);
            string resourcePath = GetResourceDataPath(resource);

            var loadResult = resourceData.Load(resource, resourcePath);
            if (loadResult.IsFailure)
            {
                return Result<ResourceData>.Fail($"Failed to load resource data: {resource}")
                    .WithErrors(loadResult);
            }

            _resourceDataCache[resource] = resourceData;

            return Result<ResourceData>.Ok(resourceData);
        }
        catch (Exception ex)
        {
            return Result<ResourceData>.Fail($"An exception occurred when loading resource data: '{resource}'")
                .WithException(ex);
        }
    }

    private string GetResourceDataPath(ResourceKey resourceKey)
    {
        var projectDataFolderPath = _projectService.CurrentProject!.ProjectDataFolderPath;
        var path = Path.Combine(projectDataFolderPath, "ResourceData", $"{resourceKey}.json");

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
