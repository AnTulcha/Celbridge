using CommunityToolkit.Diagnostics;

namespace Celbridge.Workspace.Services;

public class WorkspaceSettingsService : IWorkspaceSettingsService, IDisposable
{
    public IWorkspaceSettings? WorkspaceSettings { get; private set; }

    public string? WorkspaceSettingsFolderPath { get; set; }

    public async Task<Result> AcquireWorkspaceSettingsAsync()
    {
        if (string.IsNullOrEmpty(WorkspaceSettingsFolderPath))
        {
            return Result.Fail("The workspace settings folder has not been set.");
        }
        var databaseFilePath = Path.Combine(WorkspaceSettingsFolderPath, FileNameConstants.WorkspaceSettingsFile);

        //
        // Create the workspace settings database if it doesn't exist yet
        //

        if (!File.Exists(databaseFilePath))
        {
            var createResult = await CreateWorkspaceSettingsAsync(databaseFilePath);
            if (createResult.IsFailure)
            {
                return createResult;
            }
        }

        //
        // Load the workspace settings database
        //

        var loadResult = LoadWorkspaceSettings(databaseFilePath);
        if (loadResult.IsFailure)
        {
            return loadResult;
        }

        return Result.Ok();
    }

    public async Task<Result> CreateWorkspaceSettingsAsync(string databasePath)
    {
        try
        {
            var createResult = await Services.WorkspaceSettings.CreateWorkspaceSettingsAsync(databasePath);
            return createResult;
        }
        catch (Exception ex) 
        {
            return Result.Fail($"Failed to create workspace settings database. {ex.Message}");
        }
    }

    public Result LoadWorkspaceSettings(string databasePath)
    {
        try
        {
            var loadResult = Services.WorkspaceSettings.LoadWorkspaceSettings(databasePath);
            if (loadResult.IsFailure)
            {
                return Result.Fail($"Failed to load workspace settings database: {databasePath}");
            }

            WorkspaceSettings = loadResult.Value;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex, $"An exception occurred when loading the workspace settings database. {ex.Message}");
        }
    }

    public Result UnloadWorkspaceSettings()
    {
        if (WorkspaceSettings is null)
        {
            Guard.IsNull(WorkspaceSettings);

            // Unloading a workspace that is not loaded is a no-op
            return Result.Ok();
        }

        var disposable = WorkspaceSettings as IDisposable;
        Guard.IsNotNull(disposable);
        disposable.Dispose();
        WorkspaceSettings = null;

        return Result.Ok();
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (WorkspaceSettings is not null)
                {
                    UnloadWorkspaceSettings();
                }
            }

            _disposed = true;
        }
    }

    ~WorkspaceSettingsService()
    {
        Dispose(false);
    }
}