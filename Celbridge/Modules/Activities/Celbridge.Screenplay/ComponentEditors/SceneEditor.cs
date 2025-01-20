using Celbridge.Entities;
using Celbridge.Screenplay.ComponentEditorViews;

namespace Celbridge.Screenplay.Components;

public class SceneEditor : IComponentEditor
{
    public const string ComponentType = "Scene";
    public const string SceneTitle = "/sceneTitle";
    public const string SceneDescription = "/sceneDescription";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Scene.json";
    public Type EditorViewType => typeof(SceneEditorView);

    public IComponentProxy? Component { get; set; }

    public Result<object> GetProperty(string name)
    {
        // Todo: Get the value of the property with the given name.
        var value = $"{nameof(SceneEditor)}";
        return Result<object>.Ok(value);
    }

    public Result<bool> SetProperty(string name, object value)
    {
        // Todo: Set the value of the property with the given name.
        return Result<bool>.Ok(true);
    }
}
