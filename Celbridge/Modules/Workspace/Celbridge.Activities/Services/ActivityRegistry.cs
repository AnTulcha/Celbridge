using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Inspector;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Modules;
using Celbridge.Projects;
using Celbridge.Workspace;

namespace Celbridge.Activities.Services;

public class ActivityRegistry
{
    private readonly ILogger<ActivityRegistry> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IProjectService _projectService;
    private readonly IModuleService _moduleService;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IEntityService _entityService;
    private readonly IInspectorService _inspectorService;
    private readonly IDocumentsService _documentService;

    private ResourceKey _projectFileResource;

    private Dictionary<string, IActivity> _activities = new();
    public IReadOnlyDictionary<string, IActivity> Activities => _activities;

    public ActivityRegistry(
        ILogger<ActivityRegistry> logger,
        IMessengerService messengerService,
        IModuleService moduleService,
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        _messengerService = messengerService;
        _moduleService = moduleService;
        _projectService = projectService;
        _resourceRegistry = workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
        _documentService = workspaceWrapper.WorkspaceService.DocumentsService;
        _inspectorService = workspaceWrapper.WorkspaceService.InspectorService;
    }

    public async Task<Result> Initialize()
    {
        // Get the project resource

        var projectPath = _projectService.CurrentProject!.ProjectFilePath;
        var getResourceResult = _resourceRegistry.GetResourceKey(projectPath);
        if (getResourceResult.IsFailure)
        {
            return Result.Fail("Failed to get the project resource key")
                .WithErrors(getResourceResult);
        }
        _projectFileResource = getResourceResult.Value;

        // Populate the list of supported activities

        var populateResult = await UpdateActivities();
        if (populateResult.IsFailure)
        {
            return Result.Fail("Failed to populate activity list")
                .WithErrors(populateResult);
        }

        // Start listening for changes to the project file entity

        _messengerService.Register<ResourceKeyChangedMessage>(this, OnResourceKeyChangedMessage);
        _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);

        return Result.Ok();
    }

    public void Uninitialize()
    {
        _messengerService.Unregister<ResourceKeyChangedMessage>(this);
        _messengerService.Unregister<ComponentChangedMessage>(this);
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        var resource = message.Resource;
        var propertyPath = message.PropertyPath;

        if (resource == _projectFileResource &&
            propertyPath == "/")
        {
            _ = UpdateActivities();
        }
    }

    private void OnResourceKeyChangedMessage(object recipient, ResourceKeyChangedMessage message)
    {
        if (message.SourceResource == _projectFileResource)
        {
            // Project file has been renamed or moved
            _projectFileResource = message.DestResource;

            _ = UpdateActivities();
        }
    }

    private async Task<Result> UpdateActivities()
    {
        // Determine the set of required activities based on the project file entity components
        var getCountResult = _entityService.GetComponentCount(_projectFileResource);
        if (getCountResult.IsFailure)
        {
            return Result.Fail($"Failed to get component count for project file resource: '{_projectFileResource}'")
                .WithErrors(getCountResult);
        }
        var componentCount = getCountResult.Value;

        var requiredActivityNames = new HashSet<string>();

        for (int i = 0; i < componentCount; i++)
        {
            var getInfoResult = _entityService.GetComponentInfo(_projectFileResource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail($"Failed to get component info for component index '{i}' on project file resource: '{_projectFileResource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            bool isActivity = componentInfo.GetBooleanAttribute("isActivityComponent");
            if (!isActivity)
            {
                // Ignore non-activity components
                continue;
            }

            // Add this activity name to the list of required activities

            var activityName = componentInfo.ComponentType.Replace("Activity", string.Empty);
            requiredActivityNames.Add(activityName);
        }

        // Stop and remove activities that are no longer required.
        var runningActivityNames = _activities.Keys.ToList();
        foreach (var activityName in runningActivityNames)
        {
            if (!requiredActivityNames.Contains(activityName))
            {
                if (_activities.TryGetValue(activityName, out var activity))
                {
                    var stopResult = await activity.Stop();
                    if (stopResult.IsFailure)
                    {
                        return Result.Fail($"Failed to stop activity: '{activityName}'")
                            .WithErrors(stopResult);
                    }
                    _activities.Remove(activityName);
                }
            }
        }

        // Todo: We probably shouldn't allow any activities to run until the project components are all configured correctly.

        // Start new required activities
        foreach (var activityName in requiredActivityNames)
        {
            if (!_activities.ContainsKey(activityName))
            {
                if (!_moduleService.IsActivitySupported(activityName))
                {
                    _logger.LogError($"Activity '{activityName}' is not supported by any loaded module.");
                    continue;
                }

                var createActivityResult = _moduleService.CreateActivity(activityName);
                if (createActivityResult.IsFailure)
                {
                    _logger.LogError($"Failed to create activity '{activityName}'.");
                    continue;
                }

                var createdActivity = createActivityResult.Value;
                _activities[activityName] = createdActivity;

                var startResult = await createdActivity.Start();
                if (startResult.IsFailure)
                {
                    return Result.Fail($"Failed to start activity '{activityName}'")
                        .WithErrors(startResult);
                }
            }
        }

        return Result.Ok();
    }
}
