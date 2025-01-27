using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class ScreenplayActivityEditor : ComponentEditorBase
{
    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.ScreenplayActivity.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, string.Empty);

        return Result<ComponentSummary>.Ok(summary);
    }
}
