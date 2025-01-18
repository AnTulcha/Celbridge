using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class LineEditor : IComponentEditor
{    
    public const string ComponentType = "Line";
    public const string Character = "/character";
    public const string SourceText = "/sourceText";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Line.json";
}
