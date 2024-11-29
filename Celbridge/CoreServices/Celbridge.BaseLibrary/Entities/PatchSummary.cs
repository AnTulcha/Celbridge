namespace Celbridge.Entities;

/// <summary>
/// Describes the changes applied by a patch to an entity data object.
/// Includes a JSON Patch that can be used to reverse the changes.
/// </summary>
public record PatchSummary(string Patch, string ReversePatch, List<ComponentChangedMessage> ComponentChangeMessages);
