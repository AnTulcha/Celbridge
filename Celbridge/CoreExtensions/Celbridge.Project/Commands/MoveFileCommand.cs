﻿using Celbridge.BaseLibrary.Commands;
using Celbridge.BaseLibrary.Dialog;
using Celbridge.BaseLibrary.Project;
using Celbridge.BaseLibrary.Workspace;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Localization;

namespace Celbridge.Project.Commands;

public class MoveFileCommand : CommandBase, IMoveFileCommand
{
    public override string StackName => CommandStackNames.Project;

    public string FromResourcePath { get; set; } = string.Empty;
    public string ToResourcePath { get; set; } = string.Empty;

    private readonly IWorkspaceWrapper _workspaceWrapper;
    private readonly IProjectDataService _projectDataService;
    private readonly IDialogService _dialogService;
    private readonly IStringLocalizer _stringLocalizer;

    public MoveFileCommand(
        IWorkspaceWrapper workspaceWrapper,
        IProjectDataService projectDataService,
        IDialogService dialogService,
        IStringLocalizer stringLocalizer)
    {
        _workspaceWrapper = workspaceWrapper;
        _projectDataService = projectDataService;
        _dialogService = dialogService;
        _stringLocalizer = stringLocalizer;
    }

    public override async Task<Result> ExecuteAsync()
    {
        if (!_workspaceWrapper.IsWorkspacePageLoaded)
        {
            return Result.Fail($"Failed to move file. Workspace is not loaded");
        }

        var createResult = await MoveFileInternal(FromResourcePath, ToResourcePath);
        if (createResult.IsFailure)
        {
            var titleString = _stringLocalizer.GetString("ResourceTree_MoveFile");
            var messageString = _stringLocalizer.GetString("ResourceTree_MoveFileFailed", FromResourcePath, ToResourcePath);

            // Show alert
            await _dialogService.ShowAlertDialogAsync(titleString, messageString);
        }

        return createResult;
    }

    public override async Task<Result> UndoAsync()
    {
        if (!_workspaceWrapper.IsWorkspacePageLoaded)
        {
            return Result.Fail($"Failed to undo file move. Workspace is not loaded");
        }

        var createResult = await MoveFileInternal(ToResourcePath, FromResourcePath);
        if (createResult.IsFailure)
        {
            var titleString = _stringLocalizer.GetString("ResourceTree_MoveFile");
            var messageString = _stringLocalizer.GetString("ResourceTree_MoveFileFailed", ToResourcePath, FromResourcePath);

            // Show alert
            await _dialogService.ShowAlertDialogAsync(titleString, messageString);
        }

        return createResult;
    }

    /// <summary>
    /// Move the file at resourcePathA to resourcePathB.
    /// </summary>
    private async Task<Result> MoveFileInternal(string resourcePathA, string resourcePathB)
    {
        if (resourcePathA == resourcePathB)
        {
            // Moving to the same resource path is a no-op
            return Result.Ok();
        }

        var workspaceService = _workspaceWrapper.WorkspaceService;
        var resourceRegistry = workspaceService.ProjectService.ResourceRegistry;
        var loadedProjectData = _projectDataService.LoadedProjectData;

        Guard.IsNotNull(loadedProjectData);

        //
        // Validate the resource paths
        //

        if (string.IsNullOrEmpty(resourcePathA) ||
            string.IsNullOrEmpty(resourcePathB))
        {
            return Result.Fail("Failed to move file. Resource path is empty.");
        }

        //
        // Move the file on disk
        //

        try
        {
            var projectFolder = loadedProjectData.ProjectFolder;
            var filePathA = Path.Combine(projectFolder, resourcePathA);
            filePathA = Path.GetFullPath(filePathA); // Make separators consistent
            var filePathB = Path.Combine(projectFolder, resourcePathB);
            filePathB = Path.GetFullPath(filePathB);

            if (!File.Exists(filePathA))
            {
                return Result.Fail($"Failed to move file. File does not exist: {resourcePathA}");
            }

            if (File.Exists(filePathB))
            {
                return Result.Fail($"Failed to move file. File already exists: {resourcePathB}");
            }

            var parentFolderB = Path.GetDirectoryName(filePathB);
            if (!Directory.Exists(parentFolderB))
            {
                // The parent folder of the target file path must exist before moving the file.
                // It would be easy to create the missing parent folder(s) here automatically, but it's hard to
                // undo this operation robustly so we just don't allow it.
                return Result.Fail($"Failed to move file. Target folder does not exist: {parentFolderB}");
            }

            File.Move(filePathA, filePathB);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to move file. {ex.Message}");
        }

        //
        // Update the resource registry to move the file resource
        //

        var updateResult = resourceRegistry.UpdateRegistry();
        if (updateResult.IsFailure)
        {
            return Result.Fail($"Failed to move file. {updateResult.Error}");
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    public static void MoveFile(string fromResourcePath, string toResourcePath)
    {
        if (Path.GetExtension(fromResourcePath) != Path.GetExtension(toResourcePath))
        {
            // If the "from" and "to" file extensions don't match then we assume that the user intended to
            // move the file to a folder rather than to a file with no extension.
            // This means that this command cannot be used to remove the file extension from a file.
            toResourcePath = Path.Combine(toResourcePath, Path.GetFileName(fromResourcePath));
        }

        var commandService = ServiceLocator.ServiceProvider.GetRequiredService<ICommandService>();
        commandService.Execute<IMoveFileCommand>(command =>
        {
            command.FromResourcePath = fromResourcePath;
            command.ToResourcePath = toResourcePath;
        });
    }
}