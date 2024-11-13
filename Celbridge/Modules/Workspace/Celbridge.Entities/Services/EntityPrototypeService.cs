using Json.Pointer;
using System.Text.Json.Nodes;

namespace Celbridge.Entities.Services;

public class EntityPrototypeService
{
    private readonly Dictionary<string, JsonObject> _prototypes = new();

    public Result AddPrototype(string prototypeJson)
    {
        try
        {
            var jsonNode = JsonNode.Parse(prototypeJson);
            if (jsonNode is not JsonObject jsonObject)
            {
                return Result.Fail("Failed to parse prototype JSON as an object");
            }

            var schemaNamePointer = JsonPointer.Parse("/properties/_schemaName/const");
            if (!schemaNamePointer.TryEvaluate(jsonNode, out var schemaNameNode) ||
                schemaNameNode is null)
            {
                return Result.Fail("Schema name not found in the schema JSON");
            }

            var schemaName = schemaNameNode.GetValue<string>();
            if (string.IsNullOrEmpty(schemaName))
            {
                return Result.Fail("Schema name is empty");
            }

            // The prototype JSON has already been validated against the schema, so there's no
            // need to do it again here.

            _prototypes[schemaName] = jsonObject;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding prototype.")
                .WithException(ex);
        }
    }
}
