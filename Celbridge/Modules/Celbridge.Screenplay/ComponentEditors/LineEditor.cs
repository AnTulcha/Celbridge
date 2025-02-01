using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class LineEditor : ComponentEditorBase
{
    public const string ComponentType = "Screenplay.Line";
    public const string Character = "/character";
    public const string SourceText = "/sourceText";

    public override string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Line.json";

    public override ComponentSummary GetComponentSummary()
    {
        var character = GetString(Character);
        var sourceText = GetString(SourceText);

        var summaryText = $"{character}: {sourceText}";
        var summary = new ComponentSummary(summaryText, summaryText);

        return summary;
    }
}

