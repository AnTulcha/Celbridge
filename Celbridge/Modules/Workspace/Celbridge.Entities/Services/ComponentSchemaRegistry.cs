using Celbridge.Entities.Models;
using System.Text.Json;

namespace Celbridge.Entities.Services;

public class ComponentSchemaRegistry
{
    private const string EntityConfigFolder = "EntityConfig";
    private const string EntitySchemasFolder = "ComponentSchemas";

    private readonly Dictionary<string, ComponentSchema> _componentSchemas = new();

    public async Task<Result> LoadComponentSchemasAsync()
    {
        try
        {
            List<string> jsonContents = new List<string>();

            // The Uno docs only discuss using StorageFile.GetFileFromApplicationUriAsync()
            // to load files from the app package, but Package.Current.InstalledLocation appears
            // to work fine on both Windows and Skia+Gtk platforms.
            // https://platform.uno/docs/articles/features/file-management.html#support-for-storagefilegetfilefromapplicationuriasync
            var configFolder = await Package.Current.InstalledLocation.GetFolderAsync(EntityConfigFolder);
            var schemasFolder = await configFolder.GetFolderAsync(EntitySchemasFolder);

            var jsonFiles = await schemasFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var content = await FileIO.ReadTextAsync(jsonFile);

                AddComponentSchema(content);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading schemas")
                .WithException(ex);
        }
    }

    public Result<ComponentSchema> GetSchemaForComponentType(string componentType)
    {
        if (!_componentSchemas.TryGetValue(componentType, out var entitySchema))
        {
            return Result<ComponentSchema>.Fail($"Component schema '{componentType}' not found");
        }

        return Result<ComponentSchema>.Ok(entitySchema);
    }

    public Result<ComponentSchema> GetComponentSchemaFromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return Result<ComponentSchema>.Fail("Failed to parse JSON as an object");
            }

            if (!root.TryGetProperty("_componentType", out var componentTypeElement) 
                || componentTypeElement.ValueKind != JsonValueKind.String)
            {
                return Result<ComponentSchema>.Fail("Component type not found");
            }

            var componentType = componentTypeElement.GetString();
            if (string.IsNullOrEmpty(componentType))
            {
                return Result<ComponentSchema>.Fail("Component type is empty");
            }

            return GetSchemaForComponentType(componentType);
        }
        catch (Exception ex)
        {
            return Result<ComponentSchema>.Fail("An exception occurred when getting schema from JSON.")
                .WithException(ex);
        }
    }

    public Result AddComponentSchema(string schemaJson)
    {
        try
        {
            var createResult = ComponentSchema.FromJson(schemaJson);
            if (!createResult.IsSuccess)
            {
                return Result.Fail("Failed to create entity schema from JSON")
                    .WithErrors(createResult);
            }

            var componentSchema = createResult.Value;

            var componentType = componentSchema.ComponentType;
            if (_componentSchemas.ContainsKey(componentType))
            {
                return Result.Fail($"Component schema '{componentType}' already exists");
            }

            _componentSchemas[componentType] = componentSchema;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An exception occurred when adding the component schema.")
                .WithException(ex);
        }
    }
}
