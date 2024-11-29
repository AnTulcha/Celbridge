using System.Text.Json.Nodes;

namespace Celbridge.Entities.Models;

public class ComponentData
{
    public JsonObject JsonObject { get; private set; }
    public ComponentSchema ComponentSchema { get; }

    private ComponentData(JsonObject jsonObject, ComponentSchema componentSchema)
    {
        JsonObject = jsonObject;
        ComponentSchema = componentSchema;
    }

    public static ComponentData Create(JsonObject jsonObject, ComponentSchema componentSchema)
    {
        return new ComponentData(jsonObject, componentSchema);
    }
}
