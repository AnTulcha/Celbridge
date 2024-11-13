using System.Text.Json.Nodes;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Entities.Models;

public class Entity
{
    public EntityData Data { get; }

    public Entity(EntityData data)
    {
        Data = data;
    }

    public static Entity Create(EntityData prototype)
    {
        var newJsonObject = prototype.JsonObject.DeepClone() as JsonObject;
        Guard.IsNotNull(newJsonObject);

        var data = EntityData.Create(newJsonObject, prototype.EntitySchema);

        return new Entity(data);
    }
}
