namespace Celbridge.Entities;

/// <summary>
/// Describes the changes applied by a patch to an entity.
/// Includes both the original JSON patch and a reverse patch that can be used to undo the changes.
/// </summary>
public record PatchSummary(string Patch, string ReversePatch, List<ComponentChangedMessage> ComponentChangeMessages);
