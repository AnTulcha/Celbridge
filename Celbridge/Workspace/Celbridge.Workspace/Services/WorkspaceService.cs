using Celbridge.Activities;
using Celbridge.Console;
using Celbridge.DataTransfer;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.GenerativeAI;
using Celbridge.Inspector;
using Celbridge.Logging;
using Celbridge.Projects;
using Celbridge.Python;
using Celbridge.Settings;
using Celbridge.Status;

namespace Celbridge.Workspace.Services;

public class WorkspaceService : IWorkspaceService, IDisposable
{
    private const string ExpandedFoldersKey = "ExpandedFolders";

    private readonly ILogger<WorkspaceService> _logger;
    private readonly IEditorSettings _editorSettings;

    public IWorkspaceSettingsService WorkspaceSettingsService { get; }
    public IWorkspaceSettings WorkspaceSettings => WorkspaceSettingsService.WorkspaceSettings!;
    public IPythonService PythonService { get; }
    public IConsoleService ConsoleService { get; }
    public IDocumentsService DocumentsService { get; }
    public IInspectorService InspectorService { get; }
    public IExplorerService ExplorerService { get; }

//  Future support:    
//  public ISearchService SearchService { get; }
//  public IDebugService DebugService { get; }
//  public IRevisionControlService RevisionControlService { get; }

    public IStatusService StatusService { get; }
    public IDataTransferService DataTransferService { get; }
    public IEntityService EntityService { get; }
    public IGenerativeAIService GenerativeAIService { get; }
    public IActivityService ActivityService { get; }

    public WorkspacePanel ActivePanel { get; set; }

    private bool _workspaceStateIsDirty;

    private bool _showToolsPanelOnExitFocusMode;

    public WorkspaceService(
        IServiceProvider serviceProvider,
        ILogger<WorkspaceService> logger,
        IEditorSettings editorSettings,
        IProjectService projectService)
    {
        _logger = logger;
        _editorSettings = editorSettings;

        ContextAreaUsageDetails = new ContextAreaUsage();

        // Create instances of the required sub-services

        WorkspaceSettingsService = serviceProvider.GetRequiredService<IWorkspaceSettingsService>();
        PythonService = serviceProvider.GetRequiredService<IPythonService>();
        ConsoleService = serviceProvider.GetRequiredService<IConsoleService>();
        DocumentsService = serviceProvider.GetRequiredService<IDocumentsService>();
        InspectorService = serviceProvider.GetRequiredService<IInspectorService>();
        ExplorerService = serviceProvider.GetRequiredService<IExplorerService>();
        StatusService = serviceProvider.GetRequiredService<IStatusService>();
        DataTransferService = serviceProvider.GetRequiredService<IDataTransferService>();
        EntityService = serviceProvider.GetRequiredService<IEntityService>();
        GenerativeAIService = serviceProvider.GetRequiredService<IGenerativeAIService>();
        ActivityService = serviceProvider.GetRequiredService<IActivityService>();

        //
        // Let the workspace settings service know where to find the workspace settings database
        //

        var project = projectService.CurrentProject;
        Guard.IsNotNull(project);
        var workspaceSettingsFolder = Path.Combine(project.ProjectFolderPath, ProjectConstants.MetaDataFolder, ProjectConstants.CacheFolder);
        Guard.IsNotNullOrEmpty(workspaceSettingsFolder);
        WorkspaceSettingsService.WorkspaceSettingsFolderPath = workspaceSettingsFolder;
    }

    public void ToggleFocusMode()
    {
        // Are we in focus mode?
        bool isFocusModeActive = !_editorSettings.IsContextPanelVisible &&
            !_editorSettings.IsInspectorPanelVisible;

        if (isFocusModeActive)
        {
            // Exit focus mode
            _editorSettings.IsContextPanelVisible = true;
            _editorSettings.IsInspectorPanelVisible = true;

            if (_showToolsPanelOnExitFocusMode)
            {
                // Make the tools panel visible only if the flag was set when we entered focus mode
                _editorSettings.IsToolsPanelVisible = true;
                _showToolsPanelOnExitFocusMode = false;
            }
        }
        else
        {
            // Enter focus mode

            // Remember if we should make the tools panel visible when we exit focus mode
            _showToolsPanelOnExitFocusMode = _editorSettings.IsToolsPanelVisible;

            _editorSettings.IsContextPanelVisible = false;
            _editorSettings.IsInspectorPanelVisible = false;
            _editorSettings.IsToolsPanelVisible = false;
        }
    }

    public void SetWorkspaceStateIsDirty()
    {
        _workspaceStateIsDirty = true;
    }

    public async Task<Result> UpdateWorkspaceAsync(double deltaTime)
    {
        bool failed = false;

        if (_workspaceStateIsDirty)
        {
            _workspaceStateIsDirty = false;
            
            // Todo: Save the workspace state after a delay to avoid saving too frequently
            var saveWorkspaceResult = await SaveWorkspaceStateAsync();
            if (saveWorkspaceResult.IsFailure)
            {
                failed = true;
                _logger.LogError($"Failed to save workspace state. {saveWorkspaceResult.Error}");
            }
        }

        var saveEntitiesResult = await EntityService.SaveEntitiesAsync();
        if (saveEntitiesResult.IsFailure)
        {
            failed = true;
            _logger.LogError($"Failed to save modified entities. {saveEntitiesResult.Error}");
        }

        var saveDocumentsResult = await DocumentsService.SaveModifiedDocuments(deltaTime);
        if (saveDocumentsResult.IsFailure)
        {
            failed = true;
            _logger.LogError($"Failed to save modified documents. { saveDocumentsResult.Error}");
        }

        var activitiesResult = await ActivityService.UpdateAsync();
        if (activitiesResult.IsFailure) 
        {
            failed = true;
            _logger.LogError($"Failed to update activity service. {activitiesResult.Error}");
        }

        var inspectorResult = await InspectorService.UpdateAsync();
        if (inspectorResult.IsFailure)
        {
            failed = true;
            _logger.LogError($"Failed to update inspector service. {inspectorResult.Error}");
        }

        // Todo: Clear save icon on the status bar if there are no pending saves

        if (failed)
        {
            return Result.Fail("Failed to update workspace");
        }

        return Result.Ok();
    }

    private async Task<Result> SaveWorkspaceStateAsync()
    {
        Guard.IsNotNull(WorkspaceSettings);

        // Save the expanded folders in the Resource Registry

        var resourceRegistry = ExplorerService.ResourceRegistry;
        var expandedFolders = resourceRegistry.ExpandedFolders;
        await WorkspaceSettings.SetPropertyAsync(ExpandedFoldersKey, expandedFolders);

        return Result.Ok();
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected ContextAreaUsage ContextAreaUsageDetails;

    public void ClearContextAreaUses()
    {
        ContextAreaUsageDetails = new ContextAreaUsage();
    }

    public void SetCurrentContextAreaUsage(ContextAreaUse contextAreaUse)
    {
        ContextAreaUsageDetails.SetUsage(contextAreaUse);
    }

    public void AddContextAreaUse(ContextAreaUse contextAreaUse, UIElement element)
    {
        ContextAreaUsageDetails.Add(contextAreaUse, element);
    }
    public IWorkspaceService.SetWorkspacePagePersistenceDelegate SetWorkspacePagePersistence { get; set; }


    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // We use the dispose pattern to ensure that the sub-services release all their resources when the project is closed.
                // This helps avoid memory leaks and orphaned objects/tasks when the user edits multiple projects during a session.

                (WorkspaceSettingsService as IDisposable)!.Dispose();
                (PythonService as IDisposable)!.Dispose();
                (ConsoleService as IDisposable)!.Dispose();
                (DocumentsService as IDisposable)!.Dispose();
                (InspectorService as IDisposable)!.Dispose();
                (ExplorerService as IDisposable)!.Dispose();
                (StatusService as IDisposable)!.Dispose();
                (DataTransferService as IDisposable)!.Dispose();
                (EntityService as IDisposable)!.Dispose();
                (GenerativeAIService as IDisposable)!.Dispose();
                (ActivityService as IDisposable)!.Dispose();
            }

            _disposed = true;
        }
    }

    ~WorkspaceService()
    {
        Dispose(false);
    }
}
