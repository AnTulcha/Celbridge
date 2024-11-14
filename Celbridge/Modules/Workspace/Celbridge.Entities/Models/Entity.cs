using System.Text.Json;
using CommunityToolkit.Diagnostics;

using Path = System.IO.Path;

namespace Celbridge.Entities.Models;

public class Entity
{
    public ResourceKey Resource { get; private set; }
    public string EntityDataPath { get; private set; } = string.Empty;
    
    private EntityData? _entityData;
    public EntityData EntityData => _entityData!;

    private static JsonSerializerOptions _serializationOptions = new()
    {
        WriteIndented = true
    };

    public static Entity CreateEntity(ResourceKey resource, string entityDataPath, EntityData entityData)
    {
        var entity = new Entity();
        entity._entityData = entityData;
        entity.SetResourceKey(resource, entityDataPath);
        return entity;
    }

    public void SetResourceKey(ResourceKey resource, string entityDataPath)
    {
        Resource = resource;
        EntityDataPath = entityDataPath;
    }

    public async Task<Result> SaveAsync()
    {
        try
        {
            Guard.IsNotNull(_entityData);

            var jsonContent = JsonSerializer.Serialize(_entityData.JsonObject, _serializationOptions);

            var folder = Path.GetDirectoryName(EntityDataPath);
            Guard.IsNotNull(folder);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (var writer = new StreamWriter(EntityDataPath))
            {
                await writer.WriteAsync(jsonContent);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to save entity data for '{Resource}'")
                .WithException(ex);
        }
    }
}
