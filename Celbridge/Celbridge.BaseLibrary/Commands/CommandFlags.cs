﻿namespace Celbridge.Commands;

/// <summary>
/// Specify common actions to take when executing a command.
/// </summary>
[Flags]
public enum CommandFlags
{
    None = 0,

    /// <summary>
    /// Update the resource registry after execution.
    /// </summary>
    UpdateResourceRegistry = 1 << 0,

    /// <summary>
    /// Save the workspace state after execution.
    /// </summary>
    SaveWorkspaceState = 1 << 1
}
