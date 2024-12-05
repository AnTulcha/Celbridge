namespace Celbridge.UserInterface;

/// <summary>
/// Provides support for undoing and redoing operations.
/// </summary>
public interface IUndoService
{
    /// <summary>
    /// Attempt to undo the last operation.
    /// Returns true if the undo was successful.
    /// </summary>
    Result<bool> TryUndo();

    /// <summary>
    /// Attempt to redo the last operation.
    /// Returns true if the redo was successful.
    /// </summary>
    Result<bool> TryRedo();
}
