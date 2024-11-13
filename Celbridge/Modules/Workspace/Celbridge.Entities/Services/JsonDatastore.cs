using System.Text.Json.Nodes;
using Celbridge.Entities.Models;

namespace Celbridge.Entities.Services;

// Todo: Merge with Entity class
public class JsonDatastore
{
    private JsonObject? _jsonData;

    public string SchemaName { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; } = 0;

    public Result Initialize(string json, EntitySchema entitySchema)
    {
        try
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode is null)
            {
                return Result.Fail("Failed to parse JSON data.");
            }

            var jsonObject = jsonNode as JsonObject;
            if (jsonObject is null)
            {
                return Result.Fail("Parsed JSON data is not a JSON object.");
            }

            if (!jsonObject.TryGetPropertyValue("_schemaName", out var schemaNode) 
                || schemaNode is not JsonValue schemaValue)
            {
                return Result.Fail("Missing or invalid '_schemaName' property in JSON data.");
            }

            string schemaName = schemaValue.ToString();
            if (string.IsNullOrEmpty(schemaName))
            {
                return Result.Fail("Schema name is empty");
            }

            if (!jsonObject.TryGetPropertyValue("_schemaVersion", out var versionNode)
                || versionNode is not JsonValue schemaVersionValue)
            {
                return Result.Fail("Missing or invalid '_schemaVersion' property in JSON data.");
            }

            int schemaVersion;
            if (!schemaVersionValue.TryGetValue(out schemaVersion))
            {
                return Result.Fail("The '_schemaVersion' property must be an integer.");
            }

            var validateResult = entitySchema.ValidateJsonObject(jsonObject);
            if (validateResult.IsFailure)
            {
                return Result.Fail($"Validation failed with schema '{schemaName}' v{schemaVersion}.");
            }

            SchemaName = schemaName;
            SchemaVersion = schemaVersion;

            _jsonData = jsonObject;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to initialize JSON Data: {ex.Message}")
                .WithException(ex);
        }
    }
}
