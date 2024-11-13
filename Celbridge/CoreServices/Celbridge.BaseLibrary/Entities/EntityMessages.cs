namespace Celbridge.Entities;

/// <summary>
/// A message describing a change made to an entity, including the resource key, 
/// the affected JSON pointer, and the type of change.
/// </summary>
public record EntityChangedMessage(ResourceKey Resource, string JsonPointer, EntityChangeType ChangeType);
