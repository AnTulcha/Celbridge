using Celbridge.Entities.Models;
using Celbridge.Messaging;
using Celbridge.Projects;
using Celbridge.Workspace;
using System.Collections.Concurrent;

using Path = System.IO.Path;

namespace Celbridge.Entities.Services;

public class ResourceDataService : IResourceDataService, IDisposable
{
    private readonly IMessengerService _messengerService;
    private readonly IProjectService _projectService;
    private readonly ConcurrentDictionary<ResourceKey, ResourceData> _resourceDataCache = new(); // Cache for ResourceData objects
    private readonly ConcurrentBag<ResourceKey> _modifiedResources = new(); // Track modified resources

    public ResourceDataService(
        IMessengerService messengerService,
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _messengerService = messengerService;
        _projectService = projectService;
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyName, T? defaultValue) 
        where T : notnull
    {
        var acquireResult = AcquireResourceData(resource);
        if (acquireResult.IsFailure)
        {
            var failure = Result<T>.Fail($"Failed to acquire resource data for '{resource}'")
                .WithErrors(acquireResult);

            throw new InvalidOperationException(failure.Error);
        }

        var getResult = _resourceDataCache[resource].GetProperty<T>(propertyName);
        if (getResult.IsFailure)
        {
            return defaultValue;
        }

        return getResult.Value;
    }

    public T? GetProperty<T>(ResourceKey resource, string propertyName) 
        where T : notnull
    {
        return GetProperty<T>(resource, propertyName, default(T));
    }

    public void SetProperty<T>(ResourceKey resource, string propertyName, T newValue) where T : notnull
    {
        var acquireResult = AcquireResourceData(resource);
        if (acquireResult.IsFailure)
        {
            var failure = Result<T>.Fail($"Failed to acquire resource data for '{resource}'")
                .WithErrors(acquireResult);

            throw new InvalidOperationException(failure.Error);
        }

        var setResult = _resourceDataCache[resource].SetProperty(propertyName, newValue);
        if (setResult.IsFailure)
        {
            var failure = Result<T>.Fail($"Failed to set property '{propertyName}' for '{resource}'")
                .WithErrors(setResult);

            throw new InvalidOperationException(failure.Error);
        }

        _modifiedResources.Add(resource); // Mark resource as modified
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

    /// <summary>
    /// Acquires a ResourceData object for the given resource key.
    /// If it doesn't exist in the cache, it will load it from disk or create a new one.
    /// </summary>
    private Result AcquireResourceData(ResourceKey resource)
    {
        if (_resourceDataCache.ContainsKey(resource))
        {
            return Result.Ok();
        }

        try
        {
            // Create and load the ResourceData object
            var resourceData = new ResourceData(_messengerService);
            string resourcePath = GetResourceDataPath(resource);

            var loadResult = resourceData.Load(resource, resourcePath);
            if (loadResult.IsFailure)
            {
                return loadResult;
            }

            _resourceDataCache[resource] = resourceData;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading resource data: '{resource}'")
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

    ~ResourceDataService()
    {
        Dispose(false);
    }
}
