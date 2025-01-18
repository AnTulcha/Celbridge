using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class ScreenplayActivityEditor : IComponentEditor
{
    public const string ActivityName = "Screenplay";

    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.ScreenplayActivity.json";

    public IComponentProxy? Component { get; set; }
}
