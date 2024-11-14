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

    private const string EntityConfigFolder = "EntityConfig";
    private const string SchemasFolder = "Schemas";
    private const string PrototypesFolder = "Prototypes";

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
        var loadSchemasResult = await LoadSchemasAsync();
        if (loadSchemasResult.IsFailure)
        {
            return loadSchemasResult;
        }

        var loadPrototypesResult = await LoadPrototypesAsync();
        if (loadPrototypesResult.IsFailure)
        {
            return loadPrototypesResult;
        }

        return Result.Ok();
    }

    private async Task<Result> LoadSchemasAsync()
    {
        try
        {
            List<string> jsonContents = new List<string>();

            // The Uno docs only discuss using StorageFile.GetFileFromApplicationUriAsync()
            // to load files from the app package, but Package.Current.InstalledLocation appears
            // to work fine on both Windows and Skia+Gtk platforms.
            // https://platform.uno/docs/articles/features/file-management.html#support-for-storagefilegetfilefromapplicationuriasync
            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var schemasFolder = await configFolder.GetFolderAsync(SchemasFolder);

            var jsonFiles = await schemasFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var content = await FileIO.ReadTextAsync(jsonFile);

                _schemaService.AddSchema(content);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading schemas")
                .WithException(ex);
        }
    }

    private async Task<Result> LoadPrototypesAsync()
    {
        try
        {
            List<string> jsonContents = new List<string>();

            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var prototypesFolder = await configFolder.GetFolderAsync(PrototypesFolder);

            var jsonFiles = await prototypesFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var json = await FileIO.ReadTextAsync(jsonFile);

                var getResult = _schemaService.GetSchemaFromJson(json);
                if (getResult.IsFailure)
                {
                    return Result.Fail($"Failed to get schema for prototype: {jsonFile.DisplayName}")
                        .WithErrors(getResult);
                }

                var schema = getResult.Value;

                var validateResult = schema.ValidateJson(json);
                if (validateResult.IsFailure)
                {
                    return Result.Fail($"Failed to validate prototype")
                        .WithErrors(validateResult);
                }

                var addResult = _prototypeService.AddPrototype(json, schema);
                if (addResult.IsFailure)
                {
                    return Result.Fail($"Failed to add prototype: {jsonFile.DisplayName}")
                        .WithErrors(addResult);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading prototypes")
                .WithException(ex);
        }
    }

    private Result<Entity> CreateEntity(string schemaName)
    {
        var getResult = _prototypeService.GetPrototype(schemaName);
        if (getResult.IsFailure)
        {
            return Result<Entity>.Fail($"Failed to get prototype for schema: {schemaName}")
                .WithErrors(getResult);
        }
        var prototype = getResult.Value;

        var entity = Entity.Create(prototype);

        return Result<Entity>.Ok(entity);
    }

    /// <summary>
    /// Remaps the old resource key to a new resource key.
    /// </summary>
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

    /// <summary>
    /// Saves all modified resources to disk asynchronously.
    /// </summary>
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
