using Celbridge.Entities;
using Celbridge.Screenplay.ComponentEditorViews;

namespace Celbridge.Screenplay.Components;

public class EmptyEditor : IComponentEditor
{
    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Empty.json";
    public Type EditorViewType => typeof(EmptyEditorView);

    public IComponentProxy? Component { get; set; }

    public Result<object> GetProperty(string name)
    {
        // Todo: Get the value of the property with the given name.
        var value = $"{nameof(EmptyEditor)}";
        return Result<object>.Ok(value);
    }

    public Result<bool> SetProperty(string name, object value)
    {
        // Todo: Set the value of the property with the given name.
        return Result<bool>.Ok(true);
    }
}
