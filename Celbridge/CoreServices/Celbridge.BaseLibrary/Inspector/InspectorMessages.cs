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

/// <summary>
/// A message sent the the component type input text changes.
/// This is sent as the user enters text into the component type input box.
/// </summary>
public record ComponentTypeInputTextChangedMessage(string ComponentType);

/// <summary>
/// A message sent when the user presses the enter key in the component type input box.
/// </summary>
public record ComponentTypeEnteredMessage();

