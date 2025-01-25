using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class LineEditor : ComponentEditorBase
{
    public const string ComponentType = "Screenplay.Line";
    public const string Character = "/character";
    public const string SourceText = "/sourceText";

    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Line.json";

    public override Result<ComponentSummary> GetComponentSummary()
    {
        string formJson = """
        [
            {
              "element": "TextBlock",
              "text": "Line summary"
            }
        ]
        """;

        var summary = new ComponentSummary(0, string.Empty, ComponentStatus.Valid, formJson);

        return Result<ComponentSummary>.Ok(summary);
    }
}

