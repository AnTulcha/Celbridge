﻿using Celbridge.Console;
using Celbridge.DataTransfer;
using Celbridge.Documents;
using Celbridge.Explorer;
using Celbridge.Status;

namespace Celbridge.Workspace;

/// <summary>
/// Service for interacting with the sub-services of a loaded workspace.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Returns the Workspace Data Service associated with the workspace.
    /// </summary>
    IWorkspaceDataService WorkspaceDataService { get; }

    /// <summary>
    /// Returns the Console Service associated with the workspace.
    /// </summary>
    IConsoleService ConsoleService { get; }

    /// <summary>
    /// Returns the Documents Service associated with the workspace.
    /// </summary>
    IDocumentsService DocumentsService { get; }

    /// <summary>
    /// Returns the Explorer Service associated with the workspace.
    /// </summary>
    IExplorerService ExplorerService { get; }

    /// <summary>
    /// Returns the Status Service associated with the workspace.
    /// </summary>
    IStatusService StatusService { get; }

    /// <summary>
    /// Returns the Data Transfer Service associated with the workspace.
    /// </summary>
    IDataTransferService DataTransferService { get; }

    /// <summary>
    /// Set a flag to indicate that the workspace state is dirty and needs to be saved.
    /// </summary>
    void SetWorkspaceStateIsDirty();

    /// <summary>
    /// Save any pending workspace data changes to disk.
    /// </summary>
    Task<Result> FlushPendingSaves(double deltaTime);
}