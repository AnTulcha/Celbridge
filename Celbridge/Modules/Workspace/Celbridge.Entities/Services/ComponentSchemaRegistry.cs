using Celbridge.Entities.Models;

using Path = System.IO.Path;

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

    public async Task<Result> LoadComponentSchemasAsync()
    {
        try
        {
            var descriptorTypes = GetDescriptorTypes();

            // Load the component schemas from the app package

            // The Uno docs only discuss using StorageFile.GetFileFromApplicationUriAsync()
            // to load files from the app package, but Package.Current.InstalledLocation appears
            // to work fine on both Windows and Skia+Gtk platforms.
            // https://platform.uno/docs/articles/features/file-management.html#support-for-storagefilegetfilefromapplicationuriasync
            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConstants.ComponentConfigFolder);
            var schemasFolder = await configFolder.GetFolderAsync(EntityConstants.SchemasFolder);
            var jsonFiles = await schemasFolder.GetFilesAsync();

            foreach (var jsonFile in jsonFiles)
            {
                var componentType = Path.GetFileNameWithoutExtension(jsonFile.Name);
                var content = await FileIO.ReadTextAsync(jsonFile);

                var addResult = AddComponentSchema(componentType, content, descriptorTypes);
                if (addResult.IsFailure)
                {
                    return Result.Fail($"Failed to load component schema json file: '{jsonFile.Path}'")
                        .WithErrors(addResult);
                }
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

    public Result AddComponentSchema(string componentType, string schemaJson, Dictionary<string, Type> descriptorTypes)
    {
        try
        {
            // Instantiate a descriptor for the component type

            var componentObjectType = $"{componentType}Component";
            if (!descriptorTypes.TryGetValue(componentObjectType, out var objectType))
            {
                return Result.Fail($"Component type '{componentType}' not found in descriptor types");
            }

            var descriptor = _serviceProvider.GetRequiredService(objectType) as IComponentDescriptor;
            if (descriptor is null)
            {
                return Result.Fail($"Failed to instantiate decriptor for component type: {componentType}");
            }

            // Create the component schema

            var createResult = ComponentSchema.CreateSchema(descriptor, schemaJson);
            if (!createResult.IsSuccess)
            {
                return Result.Fail("Failed to create entity schema from JSON")
                    .WithErrors(createResult);
            }
            var componentSchema = createResult.Value;

            // Perform checks

            if (componentType != componentSchema.ComponentType)
            {
                return Result.Fail($"Component type '{componentType}' does not match type defined in schema");
            }

            if (_componentSchemas.ContainsKey(componentType))
            {
                return Result.Fail($"Component schema '{componentType}' already exists");
            }

            // Register the schema

            _componentSchemas[componentType] = componentSchema;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding the component schema.")
                .WithException(ex);
        }
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
