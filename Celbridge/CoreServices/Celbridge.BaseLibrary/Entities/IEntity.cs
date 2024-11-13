namespace Celbridge.Entities;

/// <summary>
/// Represents an entity with JSON data that can be manipulated and tracked for changes.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Initialize a new Entity object by deserializing a JSON string.
    /// The JSON data must specify an object containing a "_schema" property which defines
    /// the name of a previously registered JSON scheme used to validate changes.
    /// </summary>
    Result Initialize(string json);

    /// <summary>
    /// Sets a value in the JSON data at the specified JSON Pointer. 
    /// Validates the change against the JSON schema.
    /// </summary>
    Result Set<T>(string jsonPointer, T value);

    /// <summary>
    /// Gets a value from the JSON data at the specified JSON Pointer.
    /// </summary>
    T Get<T>(string jsonPointer);

    /// <summary>
    /// Checks if there are any operations available in the undo stack.
    /// </summary>
    bool HasUndo();

    /// <summary>
    /// Checks if there are any operations available in the redo stack.
    /// </summary>
    bool HasRedo();

    /// <summary>
    /// Reverts the last change made to the JSON data.
    /// </summary>
    Result Undo();

    /// <summary>
    /// Reapplies the last change that was undone in the JSON data.
    /// </summary>
    Result Redo();

    /// <summary>
    /// Moves a value from one JSON Pointer location to another.
    /// </summary>
    Result Move(string fromPointer, string toPointer);

    /// <summary>
    /// Copies a value from one JSON Pointer location to another.
    /// </summary>
    Result Copy(string fromPointer, string toPointer);

    /// <summary>
    /// Serializes the JSON data to a string.
    /// The serialized data contains the name of the schema used to validate changes.
    /// </summary>
    string Serialize();
}
