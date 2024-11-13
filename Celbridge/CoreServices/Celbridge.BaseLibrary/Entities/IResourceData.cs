namespace Celbridge.Entities;

public interface IResourceData
{
    T? GetProperty<T>(string propertyName, T? defaultValue) where T : notnull;

    T? GetProperty<T>(string propertyName) where T : notnull;

    void SetProperty<T>(string propertyName, T newValue) where T : notnull;
}
