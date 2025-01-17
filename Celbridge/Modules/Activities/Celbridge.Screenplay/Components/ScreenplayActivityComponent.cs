using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class ScreenplayActivityComponent : IComponentDescriptor
{
    public const string ActivityName = "Screenplay";

    public string ComponentDefinition => "Celbridge.Screenplay.Assets.Components.ScreenplayActivity.json";
}
