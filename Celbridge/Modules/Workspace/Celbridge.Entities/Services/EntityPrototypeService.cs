using Celbridge.Entities.Models;
using System.Text.Json.Nodes;

namespace Celbridge.Entities.Services;

public class EntityPrototypeService
{
    private readonly Dictionary<string, EntityData> _prototypes = new();

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
