namespace Celbridge.Entities;

/// <summary>
/// Represents a callback method that is triggered when a property in a resource is modified.
/// The callback provides the <see cref="ResourceKey"/> of the modified resource and the name of the modified property.
/// </summary>
/// <param name="resource">The resource that was modified.</param>
/// <param name="propertyName">The name of the property that was modified.</param>
public delegate void ResourcePropertyChangedNotifier(ResourceKey resource, string propertyName);

public interface IResourceDataService
{
    /// <summary>
    /// Acquire resource data for a resource.
    /// </summary>
    Result<IResourceData> AcquireResourceData(ResourceKey resource);

    /// <summary>
    /// Remaps the old resource key to a new resource key.
    /// </summary>
    Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource);

    /// <summary>
    /// Saves all modified resources to disk asynchronously.
    /// </summary>
    Task<Result> SavePendingAsync();
}

