using Celbridge.Entities.Models;
using System.Text.Json;

namespace Celbridge.Entities.Services;

public class EntitySchemaService
{
    private readonly Dictionary<string, EntitySchema> _schemas = new();

    public Result AddSchema(string schemaJson)
    {
        try
        {
            var createResult = EntitySchema.FromJson(schemaJson);
            if (!createResult.IsSuccess)
            {
                return Result.Fail("Failed to create entity schema from JSON")
                    .WithErrors(createResult);
            }

            var entitySchema = createResult.Value;

            var schemaName = entitySchema.SchemaName;
            if (_schemas.ContainsKey(schemaName))
            {
                return Result.Fail($"Schema '{schemaName}' already exists");
            }

            _schemas[schemaName] = entitySchema;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding the schema.")
                .WithException(ex);
        }
    }

    public Result<EntitySchema> GetSchemaByName(string schemaName)
    {
        if (!_schemas.TryGetValue(schemaName, out var entitySchema))
        {
            return Result<EntitySchema>.Fail($"Schema '{schemaName}' not found");
        }

        return Result<EntitySchema>.Ok(entitySchema);
    }

    public Result<EntitySchema> GetSchemaFromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Result<EntitySchema>.Fail("Failed to parse JSON as an object");
            }

            if (!root.TryGetProperty("_schemaName", out var schemaNameElement) 
                || schemaNameElement.ValueKind != JsonValueKind.String)
            {
                return Result<EntitySchema>.Fail("Schema name not found or is not a string in JSON data");
            }

            var schemaName = schemaNameElement.GetString();
            if (string.IsNullOrEmpty(schemaName))
            {
                return Result<EntitySchema>.Fail("Schema name is empty");
            }

            return GetSchemaByName(schemaName);
        }
        catch (Exception ex)
        {
            return Result<EntitySchema>.Fail("An exception occurred when getting schema from JSON.")
                .WithException(ex);
        }
    }
}
