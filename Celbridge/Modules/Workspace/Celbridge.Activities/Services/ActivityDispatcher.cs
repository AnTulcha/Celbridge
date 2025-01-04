using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Inspector;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Activities.Services;

public class ActivityDispatcher
{
    private readonly ILogger<ActivityDispatcher> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    private ActivityRegistry? _activityRegistry;

    private HashSet<ResourceKey> _pendingEntityUpdates = new();

    public ActivityDispatcher(
        ILogger<ActivityDispatcher> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;

        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
    }

    public async Task<Result> Initialize(ActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;

        _messengerService.Register<SelectedResourceChangedMessage>(this, (s, e) => HandleMessage(e.Resource));
        _messengerService.Register<ComponentChangedMessage>(this, (s, e) => HandleMessage(e.Resource));
        _messengerService.Register<PopulatedComponentListMessage>(this, (s, e) => HandleMessage(e.Resource));

        void HandleMessage(ResourceKey resource)
        {
            _pendingEntityUpdates.Add(resource);
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    public void Uninitialize()
    {
        _messengerService.Unregister<SelectedResourceChangedMessage>(this);
        _messengerService.Unregister<ComponentChangedMessage>(this);
        _messengerService.Unregister<PopulatedComponentListMessage>(this);
    }

    public async Task<Result> UpdateAsync()
    {
        Guard.IsNotNull(_activityRegistry);

        foreach (var fileResource in _pendingEntityUpdates)
        {
            // Annotate empty components

            var annotateResult = AnnotateEmptyComponents(fileResource);
            if (annotateResult.IsFailure)
            {
                return Result.Fail($"Failed to annotate empty components for resource: {fileResource}")
                    .WithErrors(annotateResult);
            }

            // Check that the entity has a Primary Component

            bool hasPrimaryComponent = _entityService.HasTag(fileResource, "PrimaryComponent");
            if (!hasPrimaryComponent)
            {
                // Todo: Annotate the entity and all components with an error message
                continue;
            }

            // Check that there is at least one component (there has to be for the entity tag to be present)

            var getCountResult = _entityService.GetComponentCount(fileResource);
            if (getCountResult.IsFailure)
            {
                return Result.Fail($"Failed to get component count for resource :{fileResource}");
            }
            var componentCount = getCountResult.Value;

            if (componentCount == 0)
            {
                return Result.Fail($"Component count is zero for resource: {fileResource}");
            }

            // Attempt to get the component info for the primary component

            var getComponentResult = _entityService.GetComponentTypeInfo(fileResource, 0);
            if (getComponentResult.IsFailure)
            {
                return Result.Fail($"Failed to get component info for resource: {fileResource}")
                    .WithErrors(getComponentResult);
            }   
            var componentInfo = getComponentResult.Value;

            if (!componentInfo.HasTag("PrimaryComponent"))
            {
                // Todo: Annotate the component with an error message
                continue;
            }

            // Get the activity name for this Primary Component

            var activityName = componentInfo.GetStringAttribute("activityName");
            if (string.IsNullOrEmpty(activityName))
            {
                return Result.Fail($"Activity name is empty for Primary Component on resource: {fileResource}");
            }

            if (!_activityRegistry.Activities.TryGetValue(activityName, out var activity))
            {
                // Todo: Annotate the component with an error message
                continue;
            }

            // Todo: Check that the Primary Component is allowed for this resource type

            // Use the activity to update the other components in the entity

            var updateResult = await activity.UpdateResourceAsync(fileResource);
            if (updateResult.IsFailure)
            {
                return Result.Fail($"Failed to update resource: {fileResource}")
                    .WithErrors(updateResult);
            }
        }

        _pendingEntityUpdates.Clear();

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result AnnotateEmptyComponents(ResourceKey resource)
    {
        var getCountResult = _entityService.GetComponentCount(resource);
        if (getCountResult.IsFailure)
        {
            return Result.Ok();
        }
        var componentCount = getCountResult.Value;

        for (int i = 0; i < componentCount; i++)
        {
            var getInfoResult = _entityService.GetComponentTypeInfo(resource, i);
            if (getInfoResult.IsFailure)
            {
                return Result.Fail(resource, $"Failed to get component type info for component index '{i}' on resource: '{resource}'")
                    .WithErrors(getInfoResult);
            }
            var componentInfo = getInfoResult.Value;

            if (componentInfo.ComponentType != "Empty")
            {
                continue;
            }

            var annotation = new ComponentAnnotation(
                ComponentStatus.Valid,
                string.Empty,
                "An empty component");

            _entityService.UpdateComponentAnnotation(resource, i, annotation);
        }

        return Result.Ok();
    }
}
