namespace Celbridge.Inspector;

public interface IFormFactory
{
    public Result<IForm> CreatePropertyForm(ResourceKey resource, int componentIndex, string propertyName);
}
