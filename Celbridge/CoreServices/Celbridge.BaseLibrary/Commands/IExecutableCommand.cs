namespace Celbridge.Commands;

/// <summary>
/// A command that can be executed via the command service.
/// </summary>
public interface IExecutableCommand
{
    /// <summary>
    /// Unique identifier for the command.
    /// </summary>
    EntityId CommandId { get; }

    /// <summary>
    /// Optional group identifier for undo/redo.
    /// Commands with the same valid group id will be undone/redone together.
    /// </summary>
    EntityId UndoGroupId { get; set; }

    /// <summary>
    /// Flags to configure behaviour when executing the command.
    /// </summary>
    CommandFlags CommandFlags { get; }

    /// <summary>
    /// Describes where in the source code the command was first executed.
    /// </summary>
    string ExecutionSource { get; set; }

    /// <summary>
    /// Called when the command is executed.
    /// Used internally by the ExecuteAsync() method.
    /// </summary>
    Action<Result>? OnExecute { get; set; }

    /// <summary>
    /// Execute the command.
    /// </summary>
    Task<Result> ExecuteAsync();

    /// <summary>
    /// Undo a previously executed command.
    /// </summary>
    Task<Result> UndoAsync();
}
