namespace Celbridge.Entities;

/// <summary>
/// Provides methods for initializing, retrieving, and manipulating entity data associated with resources.
/// </summary>
public interface IEntityService
{
    /// <summary>
    /// Initializes the Entity Service.
    /// </summary>
    Task<Result> InitializeAsync();

    /// <summary>
    /// Returns the absolute path of the Entity Data file for a resource.
    /// A path will be generated regardless of whether the resource or Entity Data file actually exist.
    /// </summary>
    string GetEntityDataPath(ResourceKey resource);

    /// <summary>
    /// Returns the project relative path of the Entity Data file for a resource.
    /// </summary>
    string GetEntityDataRelativePath(ResourceKey resource);

    /// <summary>
    /// Saves all modified entities to disk asynchronously.
    /// </summary>
    Task<Result> SaveEntitiesAsync();

    /// <summary>
    /// Move the entity data file for a resource, if one exists, to a new resource location.
    /// </summary>
    Result MoveEntityDataFile(ResourceKey sourceResource, ResourceKey destResource);

    /// <summary>
    /// Copy the Entity Data file for a resource, if one exists, to a new resource location.
    /// </summary>
    Result CopyEntityDataFile(ResourceKey sourceResource, ResourceKey destResource);

    /// <summary>
    /// Add a component to the the entity for a resource at the specified index.
    /// </summary>
    Result AddComponent(ResourceKey resource, int componentIndex, string componentType);

    /// <summary>
    /// Removes a component at the specified index from the entity for a resource.
    /// </summary>
    Result RemoveComponent(ResourceKey resource, int componentIndex);

    /// <summary>
    /// Replace the entity component for a resource at the specified index.
    /// </summary>
    Result ReplaceComponent(ResourceKey resource, int componentIndex, string componentType);

    /// <summary>
    /// Copy an entity component from a source index to a destination index.
    /// </summary>
    Result CopyComponent(ResourceKey resource, int sourceComponentIndex, int destComponentIndex);

    /// <summary>
    /// Move an entity component from a source index to a destination index.
    /// </summary>
    Result MoveComponent(ResourceKey resource, int sourceComponentIndex, int destComponentIndex);

    /// <summary>
    /// Returns the indices of the entity components of the specified type.
    /// </summary>
    Result<List<int>> GetComponentsOfType(ResourceKey resource, string componentType);

    /// <summary>
    /// Returns the number of components in the entity for a resource.
    /// </summary>
    Result<int> GetComponentCount(ResourceKey resource);

    /// <summary>
    /// Returns the component type for the entity component at the specified index.
    /// </summary>
    Result<string> GetComponentType(ResourceKey resource, int componentIndex);

    /// <summary>
    /// Returns the ComponentSchema for a component type.
    /// </summary>
    Result<ComponentSchema> GetComponentSchema(string componentType);

    /// <summary>
    /// Returns a ComponentProxy which provides a wrapper for an entity component.
    /// It is not recommended to cache the proxy as it will be invalidated on the next structural change to 
    /// the entity.
    /// </summary>
    Result<IComponentProxy> GetComponent(ResourceKey resource, int componentIndex);

    /// <summary>
    /// Returns a list of ComponentProxies which wrap every component in the entity.
    /// </summary>
    Result<IReadOnlyList<IComponentProxy>> GetComponents(ResourceKey resource);

    /// <summary>
    /// Gets the value of a property from a component.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// Returns a default value if the component or property cannot be found.
    /// </summary>
    T? GetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath, T? defaultValue) where T : notnull;

    /// <summary>
    /// Gets the value of a property from the first component of the specified type.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// Returns a default value if the component or property cannot be found.
    /// </summary>
    T? GetProperty<T>(ResourceKey resource, string componentType, string propertyPath, T? defaultValue) where T : notnull;

    /// <summary>
    /// Gets the value of a property from a component.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// Fails if the property cannot be found, or is of the wrong type.
    /// </summary>
    Result<T> GetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath) where T : notnull;

    /// <summary>
    /// Gets the value of a property from a component, returned as a JSON encoded string.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// Fails if the property cannot be found.
    /// </summary>
    Result<string> GetPropertyAsJson(ResourceKey resource, int componentIndex, string propertyPath);

    /// <summary>
    /// Replaces the value of an existing entity property for a component.
    /// If insert is true then the value is inserted at the specified key/index, rather than replacing the existing entry.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// </summary>
    Result SetProperty<T>(ResourceKey resource, int componentIndex, string propertyPath, T newValue, bool insert = false) where T : notnull;

    /// <summary>
    /// Replaces the value of an existing property from the first component of the specified type.
    /// If insert is true then the value is inserted at the specified key/index, rather than replacing the existing entry.
    /// propertyPath is a JSON Pointer (RFC 6901).
    /// </summary>
    Result SetProperty<T>(ResourceKey resource, string componentType, string propertyPath, T newValue, bool insert = false) where T : notnull;

    /// <summary>
    /// Returns the number of available undo operations for an entity.
    /// </summary>
    int GetUndoCount(ResourceKey resource);

    /// <summary>
    /// Undo the most recent entity change for a resource.
    /// </summary>
    Result UndoEntity(ResourceKey resource);

    /// <summary>
    /// Returns the number of available redo operations for an entity.
    /// </summary>
    int GetRedoCount(ResourceKey resource);

    /// <summary>
    /// Redo the most recently undone entity change for a resource.
    /// </summary>
    Result RedoEntity(ResourceKey resource);

    /// <summary>
    /// Returns true if any component in the entity has the specified tag.
    /// </summary>
    bool HasTag(ResourceKey resource, string tag);

    /// <summary>
    /// Return the list of all available component types.
    /// </summary>
    List<string> GetAllComponentTypes();
}
