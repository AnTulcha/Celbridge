using System.Runtime.CompilerServices;

namespace Celbridge.Commands;

/// <summary>
/// An asynchronous command queue service with undo/redo support.
/// </summary>
public interface ICommandService
{
    /// <summary>
    /// Create and enqueue a command that does not require configuration.
    /// May be used to execute commands with CommandFlags.Undoable enabled.
    /// </summary>
    Result Execute<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) where T : IExecutableCommand;

    /// <summary>
    /// Create and immediately execute a command that does not require configuration.
    /// Note that immediately executed commands do not support command flags or undo/redo.
    /// </summary>
    Task<Result> ExecuteNow<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) where T : IExecutableCommand;

    /// <summary>
    /// Create and asynchronously execute a command that does not require configuration.
    /// The call completes when the command has been executed.
    /// This method does not support executing undoable commands.
    /// </summary>
    Task<Result> ExecuteAsync<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand;

    /// <summary>
    /// Create, configure and enqueue a command.
    /// </summary>
    Result Execute<T> (
        Action<T> configure,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) where T : IExecutableCommand;

    /// <summary>
    /// Create, configure and immediately execute a command.
    /// Note that immediately executed commands do not support command flags or undo/redo.
    /// </summary>
    Task<Result> ExecuteNow<T>(
        Action<T> configure,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0
    ) where T : IExecutableCommand;

    /// <summary>
    /// Create, configure and asynchronously execute a command.
    /// The call completes when the command has been executed.
    /// This method does not support executing undoable commands.
    /// </summary>
    Task<Result> ExecuteAsync<T>(
        Action<T> configure,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand;

    /// <summary>
    /// Create a new command via the dependency injection system.
    /// </summary>
    T CreateCommand<T>() where T : IExecutableCommand;

    /// <summary>
    /// Add the command to the queue.
    /// It will be executed when it reaches the front of the queue.
    /// </summary>
    Result EnqueueCommand(IExecutableCommand command);

    /// <summary>
    /// Returns true if a command of the given type is in the queue.
    /// </summary>
    bool ContainsCommandsOfType<T>() where T : notnull;

    /// <summary>
    /// Removes all commands of the given type from the queue.
    /// </summary>
    void RemoveCommandsOfType<T>() where T : notnull;

    /// <summary>
    /// Returns the number of available undo operations.
    /// </summary>
    int GetUndoCount();

    /// <summary>
    /// Returns the number of available redo operations.
    /// </summary>
    int GetRedoCount();

    /// <summary>
    /// Attempt to pop the most recent undo command from the undo stack and execute it.
    /// </summary>
    Result Undo();

    /// <summary>
    /// Attempts to pop the most recently undone command from the redo stack and execute it.
    /// </summary>
    Result Redo();
}
