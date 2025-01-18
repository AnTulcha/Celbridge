using Celbridge.Entities;

namespace Celbridge.Screenplay.Components;

public class EmptyEditor : IComponentEditor
{
    public string ComponentConfigPath => "Celbridge.Screenplay.Assets.Components.Empty.json";

    public IComponentProxy? Component { get; set; }
}
