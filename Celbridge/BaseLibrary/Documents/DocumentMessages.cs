namespace Celbridge.Documents;

/// <summary>
/// A message that indicates the current number of pending document saves.
/// </summary>
public record PendingDocumentSaveMessage(int PendingSaveCount);

/// <summary>
/// A message sent when the list of opened documents changes.
/// </summary>
public record OpenDocumentsChangedMessage(List<ResourceKey> OpenDocuments);

/// <summary>
/// A message sent when the selected document changes.
/// </summary>
public record SelectedDocumentChangedMessage(ResourceKey DocumentResource);

/// <summary>
/// A message sent when an open document's resource has been renamed or moved.
/// </summary>
public record DocumentResourceChangedMessage(ResourceKey OldResource, ResourceKey NewResource);
