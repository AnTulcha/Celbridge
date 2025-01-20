using Celbridge.Entities;
using Celbridge.Screenplay.ComponentEditorViews;

namespace Celbridge.Screenplay.Components;

public class LineEditor : IComponentEditor
{    
    public const string ComponentType = "Line";
    public const string Character = "/character";
    public const string SourceText = "/sourceText";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Line.json";
    public Type EditorViewType => typeof(LineEditorView);

    public IComponentProxy? Component { get; set; }

    public Result<object> GetProperty(string name)
    {
        // Todo: Get the value of the property with the given name.
        var value = $"{nameof(LineEditor)}";
        return Result<object>.Ok(value);
    }

    public Result<bool> SetProperty(string name, object value)
    {
        // Todo: Set the value of the property with the given name.
        return Result<bool>.Ok(true);
    }
}
