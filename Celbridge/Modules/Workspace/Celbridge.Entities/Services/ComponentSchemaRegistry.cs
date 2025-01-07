using Celbridge.Entities.Models;

namespace Celbridge.Entities.Services;

public class ComponentSchemaRegistry
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, ComponentSchema> _componentSchemas = new();
    public IReadOnlyDictionary<string, ComponentSchema> ComponentSchemas => _componentSchemas;

    public ComponentSchemaRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Result LoadComponentSchemas()
    {
        try
        {
            var descriptorTypes = GetDescriptorTypes();

            foreach (var kv in descriptorTypes)
            {
                var descriptorKey = kv.Key;
                var objectType = kv.Value;

                var descriptor = _serviceProvider.GetRequiredService(objectType) as IComponentDescriptor;
                if (descriptor is null)
                {
                    return Result.Fail($"Failed to instantiate component descriptor: '{descriptorKey}'");
                }

                // Create the component schema

                var schemaJson = descriptor.SchemaJson;
                var createResult = ComponentSchema.CreateSchema(descriptor, schemaJson);
                if (!createResult.IsSuccess)
                {
                    return Result.Fail($"Failed to create component schema from JSON for component descriptor: '{descriptorKey}'")
                        .WithErrors(createResult);
                }
                var componentSchema = createResult.Value;

                // Perform checks

                // Todo: Use substring instead
                var componentType = descriptorKey.Replace("Component", string.Empty);

                if (componentType != componentSchema.ComponentType)
                {
                    return Result.Fail($"Component type '{descriptorKey}' does not match type defined in schema");
                }

                if (_componentSchemas.ContainsKey(componentType))
                {
                    return Result.Fail($"Component schema '{componentType}' already exists");
                }

                // Register the schema

                _componentSchemas[componentType] = componentSchema;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading schemas")
                .WithException(ex);
        }
    }

    public Result<ComponentSchema> GetSchemaForComponentType(string componentType)
    {
        if (!_componentSchemas.TryGetValue(componentType, out var entitySchema))
        {
            return Result<ComponentSchema>.Fail($"Component schema '{componentType}' not found");
        }

        return Result<ComponentSchema>.Ok(entitySchema);
    }

    private Dictionary<string, Type> GetDescriptorTypes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var descriptorTypes = new Dictionary<string, Type>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IComponentDescriptor).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                {
                    descriptorTypes[type.Name] = type;
                }
            }
        }

        return descriptorTypes;
    }
}
