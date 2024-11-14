using Celbridge.Entities.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Celbridge.Entities.Services;

public class EntityPrototypeService
{
    private const string EntityConfigFolder = "EntityConfig";
    private const string PrototypesFolder = "Prototypes";
    private const string FileEntityTypesFile = "FileEntityTypes.json";

    private readonly Dictionary<string, EntityData> _prototypes = new();
    private readonly Dictionary<string, List<string>> _fileEntityTypes = new();

    public async Task<Result> LoadPrototypesAsync(EntitySchemaService schemaService)
    {
        try
        {
            List<string> jsonContents = new List<string>();

            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var prototypesFolder = await configFolder.GetFolderAsync(PrototypesFolder);

            var jsonFiles = await prototypesFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var json = await FileIO.ReadTextAsync(jsonFile);

                var getResult = schemaService.GetSchemaFromJson(json);
                if (getResult.IsFailure)
                {
                    return Result.Fail($"Failed to get schema for prototype: {jsonFile.DisplayName}")
                        .WithErrors(getResult);
                }

                var schema = getResult.Value;

                var validateResult = schema.ValidateJson(json);
                if (validateResult.IsFailure)
                {
                    return Result.Fail($"Failed to validate prototype")
                        .WithErrors(validateResult);
                }

                var addResult = AddPrototype(json, schema);
                if (addResult.IsFailure)
                {
                    return Result.Fail($"Failed to add prototype: {jsonFile.DisplayName}")
                        .WithErrors(addResult);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading prototypes")
                .WithException(ex);
        }
    }

    public async Task<Result> LoadFileEntityTypesAsync()
    {
        try
        {
            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var jsonFile = await configFolder.GetFileAsync(FileEntityTypesFile);

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

                _fileEntityTypes[property.Name] = list;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading default prototypes")
                .WithException(ex);
        }
    }

    public Result AddPrototype(string prototypeJson, EntitySchema entitySchema)
    {
        try
        {
            var jsonNode = JsonNode.Parse(prototypeJson);
            if (jsonNode is not JsonObject jsonObject)
            {
                return Result.Fail("Failed to parse prototype JSON as an object");
            }

            var schemaNameValue = jsonNode["_schemaName"] as JsonValue;
            if (schemaNameValue?.TryGetValue(out string? schemaName) != true || 
                string.IsNullOrEmpty(schemaName))
            {
                return Result.Fail("Schema name is missing or empty");
            }

            // The prototype JSON has already been validated against the schema, so there's no
            // need to do it again here.

            var prototype = EntityData.Create(jsonObject, entitySchema);

            _prototypes[schemaName] = prototype;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding prototype.")
                .WithException(ex);
        }
    }

    public Result<EntityData> GetPrototype(string schemaName)
    {
        if (!_prototypes.TryGetValue(schemaName, out var prototype))
        {
            return Result<EntityData>.Fail($"Prototype for schema '{schemaName}' not found");
        }

        return Result<EntityData>.Ok(prototype);
    }


}
