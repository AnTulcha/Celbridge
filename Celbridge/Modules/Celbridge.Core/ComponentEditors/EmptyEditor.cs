using Celbridge.Entities;

namespace Celbridge.Core.Components;

public class EmptyEditor : ComponentEditorBase
{
    public override string ComponentConfigPath => "Celbridge.Core.Assets.Components.Empty.json";
    public const string Comment = "/comment";

    public override ComponentSummary GetComponentSummary()
    {
        var comment = GetString("/comment");
        var summary = new ComponentSummary(comment, comment);

        return summary;
    }
}
