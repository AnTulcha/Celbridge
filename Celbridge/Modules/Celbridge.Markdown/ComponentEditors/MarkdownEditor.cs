using Celbridge.Entities;

namespace Celbridge.Markdown.Components;

public class MarkdownEditor : ComponentEditorBase
{
    public const string ComponentType = "Markdown.Markdown";

    public override string ComponentConfigPath => "Celbridge.Markdown.Assets.Components.Markdown.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        string formJson = """
        [
            {
              "element": "TextBlock",
              "text": "Markdown summary"
            }
        ]
        """;

        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, formJson);

        return Result<ComponentSummary>.Ok(summary);
    }
}
