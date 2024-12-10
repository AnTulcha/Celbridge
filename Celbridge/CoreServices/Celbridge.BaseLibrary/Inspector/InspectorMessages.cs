namespace Celbridge.Inspector;

/// <summary>
/// A message sent when the selected component in the inspector changes.
/// </summary>
public record SelectedComponentChangedMessage(int componentIndex);

/// <summary>
/// A message sent when the target item in the inspector changes.
/// </summary>
public record InspectorTargetChangedMessage(ResourceKey Resource, int ComponentIndex);

/// <summary>
/// A message sent when the component panel edit mode changes.
/// </summary>
public record ComponentPanelModeChangedMessage(ComponentPanelMode EditMode);

public record ComponentTypeInputTextChangedMessage(string ComponentType);
