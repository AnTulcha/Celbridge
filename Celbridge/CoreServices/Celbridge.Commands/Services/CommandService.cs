using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Celbridge.Commands.Services;

using ICommandLogger = Logging.ILogger<CommandService>;
using Path = System.IO.Path;

public class CommandService : ICommandService
{
    /// <summary>
    /// Time between flushing pending saves .
    /// </summary>
    private const double FlushPendingSaveInterval = 0.2; // seconds

    private readonly ICommandLogger _logger;
    private readonly ILogSerializer _logSerializer;
    private readonly IMessengerService _messengerService;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    private record QueuedCommand(IExecutableCommand Command, CommandExecutionMode ExecutionMode);

    private readonly List<QueuedCommand> _commandQueue = new();

    private object _lock = new object();

    private readonly Stopwatch _stopwatch = new();
    private double _lastFlushTime = 0;

    private bool _stopped = false;

    private UndoStack _undoStack = new ();

    public CommandService(
        ICommandLogger logger,
        ILogSerializer logSerializer,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _logSerializer = logSerializer;
        _messengerService = messengerService;
        _workspaceWrapper = workspaceWrapper;
    }

    public Result Execute<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand
    {
        var command = CreateCommand<T>();
        command.ExecutionSource = $"{Path.GetFileName(filePath)}:{lineNumber}";
        return EnqueueCommand(command);
    }

    public async Task<Result> ExecuteNow<T>(
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand
    {
        var command = CreateCommand<T>();
        command.ExecutionSource = $"{Path.GetFileName(filePath)}:{lineNumber}";

        return await command.ExecuteAsync();
    }

    public Result Execute<T>(
        Action<T> configure,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand
    {
        var command = CreateCommand<T>();
        command.ExecutionSource = $"{Path.GetFileName(filePath)}:{lineNumber}";
        configure.Invoke(command);
        return EnqueueCommand(command);
    }

    public async Task<Result> ExecuteNow<T>(
        Action<T> configure,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) where T : IExecutableCommand
    {
        var command = CreateCommand<T>();
        command.ExecutionSource = $"{Path.GetFileName(filePath)}:{lineNumber}";
        configure.Invoke(command);

        return await command.ExecuteAsync();
    }

    public T CreateCommand<T>() where T : IExecutableCommand
    {
        var serviceProvider = ServiceLocator.ServiceProvider;
        T command = serviceProvider.GetRequiredService<T>();

        return command;
    }

    public Result EnqueueCommand(IExecutableCommand command)
    {
        // Executing a regular command (as opposed to an undo or redo) clears the redo stack.
        _undoStack.ClearRedoCommands();

        return EnqueueCommandInternal(command, CommandExecutionMode.Execute);
    }

    private Result EnqueueCommandInternal(IExecutableCommand command, CommandExecutionMode ExecutionMode)
    {
        lock (_lock)
        {
            if (_commandQueue.Any((item) => item.Command.CommandId == command.CommandId))
            {
                return Result.Fail($"Command '{command.CommandId}' is already in the execution queue");
            }

            _commandQueue.Add(new QueuedCommand(command, ExecutionMode));
        }

        return Result.Ok();
    }

    public bool ContainsCommandsOfType<T>() where T : notnull
    {
        lock (_lock)
        {
            return _commandQueue.Any(o => o is T);
        }
    }

    public void RemoveCommandsOfType<T>() where T : notnull
    {
        lock (_lock)
        {
            _commandQueue.RemoveAll(c => c.GetType().IsAssignableTo(typeof(T)));
        }
    }

    public bool IsUndoStackEmpty()
    {
        lock (_lock)
        {
            return _undoStack.GetUndoCount() == 0;
        }
    }

    public bool IsRedoStackEmpty()
    {
        lock (_lock)
        {
            return _undoStack.GetRedoCount() == 0;
        }
    }

    public Result<bool> Undo()
    {
        lock (_lock)
        {
            if (IsUndoStackEmpty())
            {
                return Result<bool>.Ok(false);
            }

            // Pop next command(s) from the undo queue
            var popResult = _undoStack.PopCommands(UndoStackOperation.Undo);
            if (popResult.IsFailure)
            {
                return Result<bool>.Fail("Failed to pop command from undo stack")
                    .WithErrors(popResult);
            }
            var commandList = popResult.Value;

            foreach (var command in commandList)
            {
                // Enqueue this command as an undo. 
                var enqueueResult = EnqueueCommandInternal(command, CommandExecutionMode.Undo);
                if (enqueueResult.IsFailure)
                {
                    return Result<bool>.Fail("Failed to enqueue popped undo command")
                        .WithErrors(enqueueResult);
                }
            }
        }

        return Result<bool>.Ok(true);
    }

    public Result<bool> Redo()
    {
        lock (_lock)
        {
            if (IsRedoStackEmpty())
            {
                return Result<bool>.Ok(false);
            }

            // Pop next command(s) from the redo queue
            var popResult = _undoStack.PopCommands(UndoStackOperation.Redo);
            if (popResult.IsFailure)
            {
                return Result<bool>.Fail("Failed to pop command from redo stack")
                    .WithErrors(popResult);
            }
            var commandList = popResult.Value;

            foreach (var command in commandList)
            {
                // Enqueue this command as a redo (same as a normal execution).
                var enqueueResult = EnqueueCommandInternal(command, CommandExecutionMode.Redo);
                if (enqueueResult.IsFailure)
                {
                    return Result<bool>.Fail("Failed to enqueue popped undo command")
                        .WithErrors(enqueueResult);
                }
            }
        }

        return Result<bool>.Ok(true);
    }

    public void StartExecution()
    {
        _ = StartExecutionAsync();
    }

    private async Task StartExecutionAsync()
    {
        _stopwatch.Start();

        while (true)
        {
            if (_stopped)
            {
                lock (_commandQueue)
                {
                    _commandQueue.Clear();
                }
                _stopped = false;
                break;
            }

            // To avoid race conditions, saving the workspace state and documents is performed while
            // there are no executing commands, and no commands are executed until saving completes.
            var flushResult = await FlushPendingSavesAsync();
            if (flushResult.IsFailure)
            {
                _logger.LogError(flushResult, "Failed to flush pending saves");
            }

            // Find the first command that is ready to execute
            IExecutableCommand? command = null;
            var executionMode = CommandExecutionMode.Execute;

            lock (_lock)
            {
                if (_commandQueue.Count > 0)
                {
                    var item = _commandQueue[0];
                    command = item.Command;
                    executionMode = item.ExecutionMode;
                    _commandQueue.RemoveAt(0);
                }
            }

            if (command is not null)
            {
                try
                {
                    // Notify listeners that the command is about to execute
                    var message = new CommandExecutingMessage(command, executionMode, (float)_stopwatch.Elapsed.TotalSeconds);
                    _messengerService.Send(message);

                    var scopeName = $"{executionMode} {command.GetType().Name}";
                    using (_logger.BeginScope(scopeName))
                    {
                        // Log the command execution at the debug level
                        string logEntry = _logSerializer.SerializeObject(message, false);
                        _logger.LogDebug(logEntry);

                        if (executionMode == CommandExecutionMode.Undo)
                        {
                            //
                            // Execute command as an undo
                            //

                            var undoResult = await command.UndoAsync();
                            await Task.Delay(1); // Workaround for a bug in the colored console logger

                            if (undoResult.IsSuccess)
                            {
                                // Push the undone command onto the redo stack
                                _undoStack.PushRedoCommand(command);
                            }
                            else
                            {
                                _logger.LogError(undoResult, "Undo command failed");
                            }
                        }
                        else
                        {
                            //
                            // Execute the command as a regular execution or redo
                            //

                            var executeResult = await command.ExecuteAsync();
                            await Task.Delay(1); // Workaround for a bug in the colored console logger

                            if (executeResult.IsSuccess)
                            {
                                if (command.CommandFlags.HasFlag(CommandFlags.Undoable))
                                {
                                    _undoStack.PushUndoCommand(command);
                                }
                            }
                            else
                            {
                                _logger.LogError(executeResult, "Execute command failed");
                            }
                        }

                        // Update the resource registry if the command requires it.
                        if (_workspaceWrapper.IsWorkspacePageLoaded &&
                            command.CommandFlags.HasFlag(CommandFlags.UpdateResources))
                        {
                            var updateResult = await UpdateResourcesAsync(command);
                            if (updateResult.IsFailure)
                            {
                                _logger.LogError(updateResult, "Update resources failed");
                            }
                        }

                        // Save the workspace state if the command requires it.
                        if (_workspaceWrapper.IsWorkspacePageLoaded &&
                            command.CommandFlags.HasFlag(CommandFlags.SaveWorkspaceState))
                        {
                            _workspaceWrapper.WorkspaceService.SetWorkspaceStateIsDirty();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // I decided not to localize this because exceptions should never occur. This is not text that the
                    // user is expected to ever see.
                    _logger.LogError(ex, $"An exception occurred when executing the command. Check the log file for more information.");
                }
            }

            await Task.Delay(1);
        }
    }

    public void StopExecution()
    {
        _stopped = true;
    }

    /// <summary>
    /// Flush any pending save operations before the next command executes.
    /// </summary>
    private async Task<Result> FlushPendingSavesAsync()
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        var deltaTime = now - _lastFlushTime;
        if (deltaTime < FlushPendingSaveInterval)
        {
            // Not enough time has passed since the last flush.
            return Result.Ok();
        }

        if (!_workspaceWrapper.IsWorkspacePageLoaded)
        {
            // No workspace is loaded, so there is nothing to save.
            return Result.Ok();
        }

        var flushResult = await _workspaceWrapper.WorkspaceService.FlushPendingSavesAsync(deltaTime);
        if (flushResult.IsFailure)
        {
            return Result.Fail($"Failed to flush pending saves")
                .WithErrors(flushResult);
        }

        // Restart the timer to account for time spent saving
        _lastFlushTime = _stopwatch.Elapsed.TotalSeconds;

        return Result.Ok();
    }

    private async Task<Result> UpdateResourcesAsync(IExecutableCommand command)
    {
        // For grouped commands, only the last command to execute should perform the
        // resource update to avoid unnecessary updates.
        // Every non-grouped command does update the resource registry. This ensures
        // that the registry & view are up to date before the next command in the queue executes.

        foreach (var item in _commandQueue)
        {
            if (item.Command.CommandFlags.HasFlag(CommandFlags.UpdateResources) &&
                item.Command.UndoGroupId == command.UndoGroupId)
            {
                return Result.Ok();
            }
        }

        var explorerService = _workspaceWrapper.WorkspaceService.ExplorerService;
        var updateResult = await explorerService.UpdateResourcesAsync();
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        return Result.Ok();
    }
}
