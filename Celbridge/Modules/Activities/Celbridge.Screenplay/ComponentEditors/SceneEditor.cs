using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class SceneEditor : IComponentEditor
{
    public const string ComponentType = "Scene";
    public const string SceneTitle = "/sceneTitle";
    public const string SceneDescription = "/sceneDescription";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Scene.json";

    public IComponentProxy? Component { get; set; }
}
