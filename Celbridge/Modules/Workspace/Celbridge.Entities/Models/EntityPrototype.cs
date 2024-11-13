using System.Text.Json.Nodes;

namespace Celbridge.Entities.Models;

public class EntityPrototype
{
    public JsonObject JsonObject { get; }
    public EntitySchema EntitySchema { get; }

    public EntityPrototype(JsonObject jsonObject, EntitySchema entitySchema)
    {
        JsonObject = jsonObject;
        EntitySchema = entitySchema;
    }
}
