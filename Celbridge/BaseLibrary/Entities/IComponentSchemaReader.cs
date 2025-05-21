namespace Celbridge.Entities;

/// <summary>
/// A factory for creating component schema readers.
/// </summary>
public interface IComponentSchemaReaderFactory
{
    /// <summary>
    /// Factory method for creating a component schema reader.
    /// </summary>
    IComponentSchemaReader Create(ComponentSchema schema);
}

/// <summary>
/// A helper utility for reading component schema information.
/// </summary>
public interface IComponentSchemaReader
{
    /// <summary>
    /// Returns true if the component type has the specified tag.
    /// Tags are defined at design time and are used to categorize component types.
    /// </summary>
    bool HasTag(string tag);

    Result<ComponentPropertyInfo> GetPropertyInfo(string propertyName);

    /// <summary>
    /// Gets a boolean attribute value at the specified property path.
    /// If property name is empty, the entity attributes are searched.
    /// Returns false if the attribute is not found or cannot be parsed.
    /// </summary>
    bool GetBooleanAttribute(string attributeName, string propertyName = "");

    /// <summary>
    /// Gets a string attribute value.
    /// If property name is empty, the entity attributes are searched.
    /// Returns an empty string if the attribute is not found.
    /// </summary>
    string GetStringAttribute(string attributeName, string propertyPath = "");

    /// <summary>
    /// Gets an integer attribute value.
    /// If property name is empty, the entity attributes are searched.
    /// Returns 0 if the attribute is not found or cannot be parsed.
    /// </summary>    
    int GetIntAttribute(string attributeName, string propertyPath = "");

    /// <summary>
    /// Gets a double attribute value.
    /// If property name is empty, the entity attributes are searched.
    /// Returns 0 if the attribute is not found or cannot be parsed.
    /// </summary>
    double GetDoubleAttribute(string attributeName, string propertyPath = "");

    /// <summary>
    /// Gets an JSON serialized object attribute value of type T.
    /// If property name is empty, the entity attributes are searched.
    /// </summary>
    Result<T> GetObjectAttribute<T>(string attributeName, string propertyPath = "") where T : notnull;
}

