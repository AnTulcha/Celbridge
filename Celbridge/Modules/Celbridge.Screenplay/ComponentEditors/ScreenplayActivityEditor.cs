using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class ScreenplayActivityEditor : ComponentEditorBase
{
    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.ScreenplayActivity.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        string formJson = """
        [
            {
              "element": "TextBlock",
              "text": "Screen play activity summary"
            }
        ]
        """;

        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, formJson);

        return Result<ComponentSummary>.Ok(summary);
    }
}
