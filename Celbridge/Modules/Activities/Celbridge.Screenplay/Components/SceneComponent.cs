using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class SceneComponent : IComponentDescriptor
{
    public const string ComponentType = "Scene";
    public const string SceneTitle = "/sceneTitle";
    public const string SceneDescription = "/sceneDescription";

    public string ComponentDefinition => "Celbridge.Screenplay.Assets.Components.Scene.json";
}
