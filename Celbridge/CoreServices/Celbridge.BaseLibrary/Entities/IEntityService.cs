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

public interface IEntityService
{
    Task<Result> InitializeAsync();

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
    /// Saves all modified entities to disk asynchronously.
    /// </summary>
    Task<Result> SaveModifiedEntities();
}

