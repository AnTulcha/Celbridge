using Celbridge.Entities;

namespace Celbridge.Markdown.Components;

public class MarkdownEditor : ComponentEditorBase
{
    public const string ComponentType = "Markdown.Markdown";

    public override string ComponentConfigPath => "Celbridge.Markdown.Assets.Components.Markdown.json";

    public override ComponentSummary GetComponentSummary()
    {
        var summary = new ComponentSummary(string.Empty, string.Empty);
        return summary;
    }
}
