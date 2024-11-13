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
    /// Gets the value of a property from a resource.
    /// </summary>
    T? GetProperty<T>(ResourceKey resource, string propertyPath, T? defaultValue) where T : notnull;

    /// <summary>
    /// Gets the value of a property from a resource.
    /// </summary>
    T? GetProperty<T>(ResourceKey resource, string propertyPath) where T : notnull;

    /// <summary>
    /// Sets the value of a property in a resource.
    /// </summary>
    void SetProperty<T>(ResourceKey resource, string propertyPath, T newValue) where T : notnull;

    /// <summary>
    /// Remaps the old resource key to a new resource key.
    /// </summary>
    Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource);

    /// <summary>
    /// Saves all modified resources to disk asynchronously.
    /// </summary>
    Task<Result> SavePendingAsync();
}

