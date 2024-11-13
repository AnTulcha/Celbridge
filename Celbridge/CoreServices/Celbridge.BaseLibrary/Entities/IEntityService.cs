namespace Celbridge.Entities;

/// <summary>
/// Enum representing the type of change made to an entity property.
/// </summary>
public enum EntityPropertyChangeType
{
    Add,
    Update,
    Remove
}

/// <summary>
/// Provides methods for managing entities.
/// </summary>
public interface IEntityService : IDisposable
{
    Task<Result> InitializeAsync();

    /// <summary>
    /// Acquires an Entity instance by its ResourceKey. Creates the Entity if it does not exist.
    /// </summary>
    IEntity AcquireEntity(ResourceKey resourceKey);

    /// <summary>
    /// Marks an Entity as modified, indicating that it requires saving.
    /// </summary>
    void MarkEntityModified(ResourceKey resourceKey);

    /// <summary>
    /// Remaps an existing Entity's ResourceKey to a new ResourceKey.
    /// </summary>
    Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource);

    /// <summary>
    /// Saves all modified Entity instances to disk asynchronously.
    /// </summary>
    Task<Result> SavePendingAsync();

    /// <summary>
    /// Destroys an Entity, removing it from the service and broadcasting a destruction event.
    /// </summary>
    void DestroyEntity(ResourceKey resourceKey);
}
