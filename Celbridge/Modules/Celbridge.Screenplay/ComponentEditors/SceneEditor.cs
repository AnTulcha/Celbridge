using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class SceneEditor : ComponentEditorBase
{
    public const string ComponentType = "Screenplay.Scene";
    public const string SceneTitle = "/sceneTitle";
    public const string SceneDescription = "/sceneDescription";

    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Scene.json";

    public override ComponentSummary GetComponentSummary()
    {
        var sceneTitle = GetString(SceneTitle);
        var sceneDescription = GetString(SceneDescription);

        var summaryText = $"{sceneTitle}: {sceneDescription}";
        var summary = new ComponentSummary(summaryText, summaryText);

        return summary;
    }
}
