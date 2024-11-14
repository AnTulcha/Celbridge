using System.Text.Json.Nodes;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Entities.Models;

public class EntityData
{
    public JsonObject JsonObject { get; }
    public EntitySchema EntitySchema { get; }

    private EntityData(JsonObject jsonObject, EntitySchema entitySchema)
    {
        JsonObject = jsonObject;
        EntitySchema = entitySchema;
    }

    public static EntityData Create(JsonObject jsonObject, EntitySchema entitySchema)
    {
        return new EntityData(jsonObject, entitySchema);
    }

    public EntityData DeepClone()
    {
        var jsonClone = JsonObject.DeepClone() as JsonObject;
        Guard.IsNotNull(jsonClone);

        return new EntityData(jsonClone, EntitySchema);
    }

    public Result Copy(string fromPointer, string toPointer)
    {
        throw new NotImplementedException();
    }

    public bool HasRedo()
    {
        throw new NotImplementedException();
    }

    public bool HasUndo()
    {
        throw new NotImplementedException();
    }

    public Result Move(string fromPointer, string toPointer)
    {
        throw new NotImplementedException();
    }

    public Result Redo()
    {
        throw new NotImplementedException();
    }

    public Result Undo()
    {
        throw new NotImplementedException();
    }
}
