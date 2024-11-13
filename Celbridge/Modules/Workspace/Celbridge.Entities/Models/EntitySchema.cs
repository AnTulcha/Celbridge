using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Celbridge.Entities.Models;

public class EntitySchema
{
    public string SchemaName { get; }
    public int SchemaVersion { get; }

    private readonly JsonSchema _jsonSchema;

    private EntitySchema(string schemaName, int schemaVersion, JsonSchema jsonSchema)
    {
        SchemaName = schemaName;
        SchemaVersion = schemaVersion;
        _jsonSchema = jsonSchema;
    }

    public static Result<EntitySchema> FromJson(string schemaJson)
    {
        try
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Result<EntitySchema>.Fail("Failed to parse schema JSON as an object");
            }

            // Get schema name
            var schemaNamePointer = JsonPointer.Parse("/properties/_schemaName/const");
            var schemaNameElement = schemaNamePointer.Evaluate(root);
            if (schemaNameElement is null)
            {
                return Result<EntitySchema>.Fail("Schema name not found in the schema JSON");
            }

            if (schemaNameElement is not JsonElement nameElement ||
                nameElement.ValueKind != JsonValueKind.String)
            {
                return Result<EntitySchema>.Fail("Schema name element is not a string");
            }

            var schemaName = nameElement.GetString();
            if (string.IsNullOrEmpty(schemaName))
            {
                return Result<EntitySchema>.Fail("Schema name is empty");
            }

            // Get schema version 
            var schemaVersionPointer = JsonPointer.Parse("/properties/_schemaVersion/const");
            var schemaVersionElement = schemaVersionPointer.Evaluate(root);
            if (schemaVersionElement is not JsonElement versionElement ||
                versionElement.ValueKind != JsonValueKind.Number)
            {
                return Result<EntitySchema>.Fail("Schema version not found or is not an integer in the schema JSON");
            }
            var schemaVersion = versionElement.GetInt32();

            // Create the JsonSchema object
            var jsonSchema = JsonSchema.FromText(schemaJson);
            if (jsonSchema is null)
            {
                return Result<EntitySchema>.Fail($"Failed to parse schema '{schemaName}'");
            }

            return Result<EntitySchema>.Ok(new EntitySchema(schemaName, schemaVersion, jsonSchema));
        }
        catch (Exception ex)
        {
            return Result<EntitySchema>.Fail("An exception occurred when parsing schema JSON.")
                .WithException(ex);
        }
    }

    public Result ValidateJsonObject(JsonObject jsonObject)
    {
        var validationResult = _jsonSchema.Evaluate(jsonObject);
        return validationResult.IsValid
            ? Result.Ok()
            : Result.Fail($"Validation failed with schema '{SchemaName}'");
    }

    public Result ValidateJson(string json)
    {
        try
        {
            var jsonObject = JsonNode.Parse(json) as JsonObject;
            if (jsonObject is null)
            {
                return Result.Fail("Failed to parse JSON data as a JSON object");
            }

            return ValidateJsonObject(jsonObject);
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when validating JSON data.")
                .WithException(ex);
        }
    }
}
