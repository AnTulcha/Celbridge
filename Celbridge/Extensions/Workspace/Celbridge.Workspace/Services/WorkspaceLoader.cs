﻿using CommunityToolkit.Diagnostics;

namespace Celbridge.Workspace.Services;

public class WorkspaceLoader
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public WorkspaceLoader(IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public async Task<Result> LoadWorkspaceAsync()
    {
        var workspaceService = _workspaceWrapper.WorkspaceService;
        if (workspaceService is null)
        {
            return Result.Fail("Workspace service is not initialized");
        }

        var explorerService = workspaceService.ExplorerService;

        //
        // Acquire the workspace database
        //
        var workspaceDataService = workspaceService.WorkspaceDataService;
        var acquireResult = await workspaceDataService.AcquireWorkspaceDataAsync();
        if (acquireResult.IsFailure)
        {
            var failure = Result.Fail("Failed to acquire the workspace data");
            failure.MergeErrors(acquireResult);
            return failure;
        }

        var workspaceData = workspaceDataService.LoadedWorkspaceData;
        Guard.IsNotNull(workspaceData);

        //
        // Populate the resource registry.
        //
        try
        {
            // Restore previous state of expanded folders before populating resources
            var expandedFolders = await workspaceData.GetPropertyAsync<List<string>>("ExpandedFolders");
            if (expandedFolders is not null &&
                expandedFolders.Count > 0)
            {
                var resourceRegistry = workspaceService.ExplorerService.ResourceRegistry;
                foreach (var expandedFolder in expandedFolders)
                {
                    resourceRegistry.SetFolderIsExpanded(expandedFolder, true);
                }
            }

            var updateResult = await explorerService.UpdateResourcesAsync();
            if (updateResult.IsFailure)
            {
                var failure = Result.Fail("Failed to update resources");
                failure.MergeErrors(updateResult);
                return failure;
            }
        }
        catch (Exception ex)
        {
            return Result.Fail(ex, $"An exception occurred while populating the resource registry");
        }

        //
        // Restore the previous state of the workspace.
        // Any failures that occur here are logged as warnings and do not prevent the workspace from loading.
        //

        // Select the previous selected resource in the Explorer Panel.
        await explorerService.RestorePanelState();

        // Open previous opened documents in the Documents Panel
        var documentsService = workspaceService.DocumentsService;
        await documentsService.RestorePanelState();

        //
        // Update the current stored state of the workspace in preparation for the next session.
        //
        await explorerService.StoreSelectedResource();
        await documentsService.StoreSelectedDocument();
        await documentsService.StoreOpenDocuments();

        return Result.Ok();
    }
}