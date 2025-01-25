using Celbridge.Entities;

namespace Celbridge.Core.Components;

public class EmptyEditor : ComponentEditorBase
{
    public override string ComponentConfigPath => "Celbridge.Core.Assets.Components.Empty.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        var getComment = GetProperty("/comment");
        if (getComment.IsFailure)
        {
            return Result<ComponentSummary>.Fail(getComment.Error);
        }
        var comment = getComment.Value;

        string formJson = $$"""
        [
            {
              "element": "TextBlock",
              "text": "{{comment}}"
            }
        ]
        """;

        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, formJson);

        return Result<ComponentSummary>.Ok(summary);
    }
}
