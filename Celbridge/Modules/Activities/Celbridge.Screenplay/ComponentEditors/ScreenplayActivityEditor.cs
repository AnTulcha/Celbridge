using Celbridge.Entities;
using Celbridge.Screenplay.ComponentEditorViews;

namespace Celbridge.Screenplay.Components;

public class ScreenplayActivityEditor : IComponentEditor
{
    public const string ActivityName = "Screenplay";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.ScreenplayActivity.json";
    public Type EditorViewType => typeof(ScreenplayActivityEditorView);

    public IComponentProxy? Component { get; set; }

    public Result<object> GetProperty(string name)
    {
        // Todo: Get the value of the property with the given name.
        var value = $"{nameof(ScreenplayActivityEditor)}";
        return Result<object>.Ok(value);
    }

    public Result<bool> SetProperty(string name, object value)
    {
        // Todo: Set the value of the property with the given name.
        return Result<bool>.Ok(true);
    }
}
