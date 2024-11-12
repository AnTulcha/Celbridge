using Json.Schema;
using System.Text.Json.Nodes;
using System.Reflection;

namespace Celbridge.ResourceData.Services;

public class SchemaService
{
    private readonly Dictionary<string, JsonSchema> _schemas = new();

    public Result AddSchema(string schemaJson)
    {
        try
        {
            var jsonNode = JsonNode.Parse(schemaJson);
            if (jsonNode is not JsonObject jsonObject)
            {
                return Result.Fail("Failed to parse schema JSON as an object");
            }

            if (!jsonObject.TryGetPropertyValue("_schemaName", out var schemaNameNode) 
                || schemaNameNode is not JsonValue schemaNameValue)
            {
                return Result.Fail("Schema name not found in the schema JSON");
            }

            var schemaName = schemaNameValue.GetValue<string>();
            if (string.IsNullOrEmpty(schemaName))
            {
                return Result.Fail("Schema name is empty");
            }

            if (_schemas.ContainsKey(schemaName))
            {
                return Result.Fail($"Schema '{schemaName}' already exists");
            }

            var schema = JsonSchema.FromText(schemaJson);
            if (schema is null)
            {
                return Result.Fail($"Failed to parse schema '{schemaName}'");
            }

            _schemas[schemaName] = schema;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when parsing schema.").WithException(ex);
        }
    }

    public Result LoadSchemaFromFile(string filePath)
    {
        try
        {
            var schemaJson = File.ReadAllText(filePath);
            return AddSchema(schemaJson);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load schema from '{filePath}'").WithException(ex);
        }
    }

    public Result LoadSchemaFromEmbeddedResource(string resourcePath, string? assemblyName = null)
    {
        try
        {
            // Resolve the assembly by name, defaulting to the executing assembly if no name is provided
            var assembly = assemblyName != null ? Assembly.Load(assemblyName) : Assembly.GetExecutingAssembly();
            if (assembly == null)
            {
                return Result.Fail($"Assembly '{assemblyName}' could not be loaded.");
            }

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream is null)
            {
                return Result.Fail($"Embedded resource '{resourcePath}' not found in assembly '{assembly.FullName}'.");
            }

            using var reader = new StreamReader(stream);
            var schemaJson = reader.ReadToEnd();
            return AddSchema(schemaJson);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load schema from embedded resource '{resourcePath}' in assembly '{assemblyName}'").WithException(ex);
        }
    }

    public Result ValidateJsonNode(JsonObject jsonObject)
    {
        if (!jsonObject.TryGetPropertyValue("_schemaName", out var schemaNameNode) 
            || schemaNameNode is not JsonValue schemaNameValue)
        {
            return Result.Fail("Schema name not found in the schema JSON");
        }

        var schemaName = schemaNameValue.GetValue<string>();
        if (string.IsNullOrEmpty(schemaName))
        {
            return Result.Fail("Schema name is empty");
        }

        if (!_schemas.TryGetValue(schemaName, out var schema))
        {
            return Result.Fail($"Schema '{schemaName}' not found");
        }

        var validationResult = schema.Evaluate(jsonObject);
        if (!validationResult.IsValid)
        {
            return Result.Fail($"Validation failed with schema '{schemaName}'");
        }

        return Result.Ok();
    }
}
