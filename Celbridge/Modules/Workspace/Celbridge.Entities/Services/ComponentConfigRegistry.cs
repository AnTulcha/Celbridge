using System.Reflection;
using Celbridge.Entities.Models;

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

    public Result Initialize()
    {
        try
        {
            var getPathsResult = GetComponentConfigPaths();
            if (getPathsResult.IsFailure)
            {
                return Result.Fail($"Failed to get component config paths")
                    .WithErrors(getPathsResult);
            }
            var configPaths = getPathsResult.Value;

            foreach (var kv in configPaths)
            {
                var editorKey = kv.Key;
                var configPath = kv.Value;

                // Create the component config

                // Load the component config JSON from an embedded resource
                var loadConfigResult = LoadComponentConfig(configPath);
                if (loadConfigResult.IsFailure)
                {
                    return Result.Fail($"Failed to load component config for component editor: '{editorKey}'")
                        .WithErrors(loadConfigResult);
                }
                var configJson = loadConfigResult.Value;

                // Create the component config
                var createResult = ComponentConfig.CreateConfig(configJson);
                if (!createResult.IsSuccess)
                {
                    return Result.Fail($"Failed to create component config from JSON for component editor: '{editorKey}'")
                        .WithErrors(createResult);
                }
                var config = createResult.Value;

                // Perform checks

                if (!editorKey.EndsWith("Editor"))
                {
                    return Result.Fail($"Component editor name does not end with 'Editor': '{editorKey}'");
                }

                // Remove trailing "Editor" from editor key to form the component type
                var componentType = editorKey[..^6];

                if (componentType != config.ComponentType)
                {
                    return Result.Fail($"Component type does not match type defined in schema: '{editorKey}'");
                }

                if (_componentConfigs.ContainsKey(componentType))
                {
                    return Result.Fail($"Component config already exists: '{componentType}'");
                }

                // Register the config

                _componentConfigs[componentType] = config;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading schema JSON files")
                .WithException(ex);
        }
    }

    private Result<string> LoadComponentConfig(string componentConfigFile)
    {
        var assembly = Assembly.Load("Celbridge.Screenplay");

        var stream = assembly.GetManifestResourceStream(componentConfigFile);
        if (stream is null)
        {
            return Result<string>.Fail($"Embedded resource not found: {componentConfigFile}");
        }

        var json = string.Empty;
        try
        {
            using (stream)
            using (StreamReader reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"An exception occurred when reading content of embedded resource: {componentConfigFile}")
                .WithException(ex);
        }

        return Result<string>.Ok(json);
    }

    public Result<ComponentConfig> GetComponentConfig(string componentType)
    {
        if (!_componentConfigs.TryGetValue(componentType, out var config))
        {
            return Result<ComponentConfig>.Fail($"Component config not found: '{componentType}'");
        }

        return Result<ComponentConfig>.Ok(config);
    }

    private Result<Dictionary<string, string>> GetComponentConfigPaths()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var configPaths = new Dictionary<string, string>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(IComponentEditor).IsAssignableFrom(type) || 
                    type.IsAbstract || 
                    !type.IsClass)
                {
                    continue;
                }

                var editor = _serviceProvider.GetRequiredService(type) as IComponentEditor;
                if (editor is null)
                {
                    return Result<Dictionary<string, string>>.Fail($"Failed to instantiate component editor type: '{type}'");
                }

                string configPath = editor.ComponentConfigPath;
                configPaths[type.Name] = configPath;
            }
        }

        return Result<Dictionary<string, string>>.Ok(configPaths);
    }
}
