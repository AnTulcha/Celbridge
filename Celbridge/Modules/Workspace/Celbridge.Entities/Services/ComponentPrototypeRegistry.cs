using Celbridge.Entities.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Celbridge.Entities.Services;

public class ComponentPrototypeRegistry
{
    private const string EntityConfigFolder = "EntityConfig";
    private const string ComponentPrototypesFolder = "ComponentPrototypes";
    private const string EntityComponentTypesFile = "EntityComponentTypes.json";

    private readonly Dictionary<string, ComponentData> _componentPrototypes = new();
    private readonly Dictionary<string, List<string>> _entityComponentTypes = new();

    public async Task<Result> LoadComponentPrototypesAsync(ComponentSchemaRegistry componentSchemaRegistry)
    {
        try
        {
            List<string> jsonContents = new List<string>();

            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var prototypesFolder = await configFolder.GetFolderAsync(ComponentPrototypesFolder);

            var jsonFiles = await prototypesFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var json = await FileIO.ReadTextAsync(jsonFile);

                var getResult = componentSchemaRegistry.GetComponentSchemaFromJson(json);
                if (getResult.IsFailure)
                {
                    return Result.Fail($"Failed to get component schema for component prototype: {jsonFile.DisplayName}")
                        .WithErrors(getResult);
                }

                var componentSchema = getResult.Value;

                var validateResult = componentSchema.ValidateJson(json);
                if (validateResult.IsFailure)
                {
                    return Result.Fail($"Failed to validate component prototype: {jsonFile.DisplayName}")
                        .WithErrors(validateResult);
                }

                var addResult = AddComponentPrototype(json, componentSchema);
                if (addResult.IsFailure)
                {
                    return Result.Fail($"Failed to add component prototype: {jsonFile.DisplayName}")
                        .WithErrors(addResult);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading component prototypes")
                .WithException(ex);
        }
    }

    public async Task<Result> LoadEntityComponentTypesAsync()
    {
        try
        {
            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var jsonFile = await configFolder.GetFileAsync(EntityComponentTypesFile);

            var content = await FileIO.ReadTextAsync(jsonFile);

            using var jsonDoc = JsonDocument.Parse(content);

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    return Result.Fail($"Expected object value for property: {property.Name}");
                }

                var list = new List<string>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        return Result.Fail($"Expected string value for property: {property.Name}");
                    }

                    list.Add(item.GetString()!);
                }

                _entityComponentTypes[property.Name] = list;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading entity component types")
                .WithException(ex);
        }
    }

    public Result AddComponentPrototype(string componentPrototypeJson, ComponentSchema componentSchema)
    {
        try
        {
            var jsonNode = JsonNode.Parse(componentPrototypeJson);
            if (jsonNode is not JsonObject jsonObject)
            {
                return Result.Fail("Failed to parse component prototype JSON as an object");
            }

            var componentTypeValue = jsonNode["_componentType"] as JsonValue;
            if (componentTypeValue?.TryGetValue(out string? componentType) != true || 
                string.IsNullOrEmpty(componentType))
            {
                return Result.Fail("Component type is missing or empty");
            }

            // The component JSON has already been validated against the schema, so there's no
            // need to do it again here.

            var componentPrototype = ComponentData.Create(jsonObject, componentSchema);

            _componentPrototypes[componentType] = componentPrototype;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding prototype.")
                .WithException(ex);
        }
    }

    public List<string> GetFileEntityTypes(string fileExtension)
    {
        if (_entityComponentTypes.TryGetValue(fileExtension, out var entityTypes))
        {
            return entityTypes;
        }

        return new List<string>();
    }

    public Result<ComponentData> GetPrototype(string componentType)
    {
        if (!_componentPrototypes.TryGetValue(componentType, out var componentPrototype))
        {
            return Result<ComponentData>.Fail($"Component prototype for entity type '{componentType}' not found");
        }

        return Result<ComponentData>.Ok(componentPrototype);
    }
}
