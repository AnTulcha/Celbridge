using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Modules;
using Celbridge.Projects;
using Celbridge.Workspace;

namespace Celbridge.Activities.Services;

public class ActivityService : IActivityService, IDisposable
{
    private ILogger<ActivityService> _logger;
    private IMessengerService _messengerService;
    private IProjectService _projectService;
    private IModuleService _moduleService;
    private IWorkspaceWrapper _workspaceWrapper;

    private ResourceKey _projectFileResource;

    private Dictionary<string, IActivity> _activities = new();

    private List<IActivity> _pendingInspectorUpdates = new();

    public ActivityService(
        ILogger<ActivityService> logger,
        IMessengerService messengerService,
        IProjectService projectService,
        IModuleService moduleService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _messengerService = messengerService;
        _projectService = projectService;
        _moduleService = moduleService;
        _workspaceWrapper = workspaceWrapper;

        _messengerService.Register<ResourceKeyChangedMessage>(this, OnResourceKeyChangedMessage);
    }

    public async Task<Result> Initialize()
    {
        var resourceRegistry = _workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        // Get the project resource

        var projectPath = _projectService.CurrentProject!.ProjectFilePath;
        var getResourceResult = resourceRegistry.GetResourceKey(projectPath);
        if (getResourceResult.IsFailure)
        {
            return Result.Fail("Failed to get the project resource key")
                .WithErrors(getResourceResult);
        }
        _projectFileResource = getResourceResult.Value;

        // Populate the list of supported activities

        var populateResult = await PopulateActivities();
        if (populateResult.IsFailure)
        {
            return Result.Fail("Failed to populate activity list")
                .WithErrors(populateResult);
        }

        return Result.Ok();
    }

    public Result RequestInpectorUpdate(string activityName)
    {
        if (!_activities.ContainsKey(activityName))
        {
            return Result.Fail($"Activity '{activityName}' does not exist");
        }

        var activity = _activities[activityName];

        // Request an update on the inspected entity from the specified activity

        if (!_pendingInspectorUpdates.Contains(activity))
        {
            // Todo: Sort the activities by priority, in case update order is important.
            _pendingInspectorUpdates.Add(activity);
        }

        return Result.Ok();
    }

    public async Task<Result> UpdateActivities()
    {
        // Update the inspected entity for each activity in the update list
        foreach (var activity in _pendingInspectorUpdates)
        {
            var updateResult = await activity.UpdateInspectedEntityAppearanceAsync();
            if (updateResult.IsFailure)
            {
                _pendingInspectorUpdates.Clear();
                return Result.Fail($"Failed to update inspected entity for activity '{activity.ActivityName}'")
                    .WithErrors(updateResult);
            }
        }

        // Reset the update list
        _pendingInspectorUpdates.Clear();

        return Result.Ok();
    }

    private void OnResourceKeyChangedMessage(object recipient, ResourceKeyChangedMessage message)
    {
        if (message.SourceResource == _projectFileResource)
        {
            // Project file has been renamed or moved
            _projectFileResource = message.DestResource;

            _ = PopulateActivities();
        }
    }

    private async Task<Result> PopulateActivities()
    {
        var entityService = _workspaceWrapper.WorkspaceService.EntityService;

        // Todo: Call a Stop() method on existing activities to allow them to exit gracefully
        _activities.Clear();

        // Get the number of components on the project file resource
        var getCountResult = entityService.GetComponentCount(_projectFileResource);
        if (getCountResult.IsFailure)
        {
            return Result.Fail($"Failed to get component count for project file resource: '{_projectFileResource}'")
                .WithErrors(getCountResult);
        }
        var componentCount = getCountResult.Value;

        for (int i = 0; i < componentCount; i++)
        {
            var getInfoResult = entityService.GetComponentTypeInfo(_projectFileResource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail($"Failed to get component info for component index '{i}' on project file resource: '{_projectFileResource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            bool isActivity = componentInfo.GetBooleanAttribute("isActivityComponent");
            if (!isActivity)
            {
                // Todo: Ignore this for now, but it should not be possible to add non-activity components to the project file
                continue;
            }

            var componentType = componentInfo.ComponentType;
            var componentIndex = i;

            // Instantiate the activity based on the activity component type

            var activityName = componentType.Replace("Activity", string.Empty);

            if (!_moduleService.IsActivitySupported(activityName))
            {
                _logger.LogError("Activity '{0}' is not supported by any loaded module.");
                continue;
            }

            var createActivityResult = _moduleService.CreateActivity(activityName);
            if (createActivityResult.IsFailure)
            {
                _logger.LogError("Failed to create activity '{0}'.", activityName);
                continue;
            }
            var createdActivity = createActivityResult.Value;

            if (_activities.ContainsKey(activityName))
            {
                _logger.LogError($"Activity '{activityName}' already exists");
                continue;
            }

            _activities[activityName] = createdActivity;
        }

        await Task.CompletedTask;

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
                // Dispose managed objects here
            }

            _disposed = true;
        }
    }

    ~ActivityService()
    {
        Dispose(false);
    }
}
