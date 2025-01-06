using Celbridge.Entities.Models;

using Path = System.IO.Path;

namespace Celbridge.Entities.Services;

public class ComponentConfigRegistry
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, ComponentConfig> _componentConfigs = new();
    public IReadOnlyDictionary<string, ComponentConfig> ComponentConfigs => _componentConfigs;

    public ComponentConfigRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> LoadComponentConfigsAsync()
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

                var addResult = AddComponentConfig(componentType, content, descriptorTypes);
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
            return Result.Fail($"An exception occurred when loading schema JSON files")
                .WithException(ex);
        }
    }

    public Result<ComponentConfig> GetComponentConfig(string componentType)
    {
        if (!_componentConfigs.TryGetValue(componentType, out var config))
        {
            return Result<ComponentConfig>.Fail($"Component config not found: '{componentType}'");
        }

        return Result<ComponentConfig>.Ok(config);
    }

    public Result AddComponentConfig(string componentType, string schemaJson, Dictionary<string, Type> descriptorObjectTypes)
    {
        try
        {
            // Instantiate a descriptor for the component type

            var descriptorKey = $"{componentType}Component";
            if (!descriptorObjectTypes.TryGetValue(descriptorKey, out var objectType))
            {
                return Result.Fail($"Component type not found in descriptor types: '{componentType}'");
            }

            var descriptor = _serviceProvider.GetRequiredService(objectType) as IComponentDescriptor;
            if (descriptor is null)
            {
                return Result.Fail($"Failed to instantiate decriptor for component type: '{componentType}'");
            }

            // Create the component config

            var createResult = ComponentConfig.CreateConfig(descriptor, schemaJson);
            if (!createResult.IsSuccess)
            {
                return Result.Fail("Failed to create component config from JSON")
                    .WithErrors(createResult);
            }
            var config = createResult.Value;

            // Perform checks

            if (componentType != config.ComponentType)
            {
                return Result.Fail($"Component type does not match type defined in component config: '{componentType}'");
            }

            if (_componentConfigs.ContainsKey(componentType))
            {
                return Result.Fail($"Component config already exists: '{componentType}'");
            }

            // Register the config

            _componentConfigs[componentType] = config;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when adding the component config for component type: {componentType}")
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
