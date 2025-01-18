using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class MarkdownEditor : IComponentEditor
{
    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Markdown.json";

    public IComponentProxy? Component { get; set; }
}
