using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class SceneEditor : ComponentEditorBase
{
    public const string ComponentType = "Screenplay.Scene";
    public const string SceneTitle = "/sceneTitle";
    public const string SceneDescription = "/sceneDescription";

    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Scene.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        string formJson = """
        [
            {
              "element": "TextBlock",
              "text": "Scene summary"
            }
        ]
        """;

        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, formJson);

        return Result<ComponentSummary>.Ok(summary);
    }
}
