namespace Celbridge.Commands;

/// <summary>
/// Base class for commands that can be executed via the command service.
/// </summary>
public abstract class CommandBase : IExecutableCommand
{
    /// <summary>
    /// Unique identifier for the command.
    /// </summary>
    public EntityId CommandId { get; } = EntityId.Create();

    /// <summary>
    /// Optional group identifier for undo/redo.
    /// Commands with the same valid group id will be undone/redone together.
    /// </summary>
    public EntityId UndoGroupId { get; set; }

    /// <summary>
    /// Flags to configure behaviour when executing the command.
    /// </summary>
    public virtual CommandFlags CommandFlags => CommandFlags.None;

    /// <summary>
    /// Describes where in the source code the command was first executed.
    /// </summary>
    public string ExecutionSource { get; set; } = string.Empty;

    /// <summary>
    /// A callback action called when the command is executed.
    /// </summary>
    public Action<Result>? OnExecute { get; set; } = null;

    /// <summary>
    /// Execute the command.
    /// </summary>
    public abstract Task<Result> ExecuteAsync();

    /// <summary>
    /// Undo a previously executed command.
    /// </summary>
    public virtual Task<Result> UndoAsync()
    {
        throw new NotImplementedException();
    }
}
