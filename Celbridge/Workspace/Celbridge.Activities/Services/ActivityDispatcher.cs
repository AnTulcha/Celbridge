using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;

namespace Celbridge.Activities.Services;

public class ActivityDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActivityDispatcher> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IEntityService _entityService;

    private ActivityRegistry? _activityRegistry;

    private List<ResourceKey> _pendingInits = new();
    private List<ResourceKey> _pendingUpdates = new();

    public ActivityDispatcher(
        IServiceProvider serviceProvider,
        ILogger<ActivityDispatcher> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _messengerService = messengerService;
        _entityService = workspaceWrapper.WorkspaceService.EntityService;
    }

    public async Task<Result> Initialize(ActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;

        _messengerService.Register<EntityCreatedMessage>(this, (s, e) => OnInitMessage(e.Resource));

        void OnInitMessage(ResourceKey resource)
        {
            if (!resource.IsEmpty)
            {
                // Make a note of the new entity for initialization
                _pendingInits.AddDistinct(resource);
            }
        }

        _messengerService.Register<SelectedResourceChangedMessage>(this, (s, e) => OnUpdateMessage(e.Resource));
        _messengerService.Register<ComponentChangedMessage>(this, (s, e) => OnUpdateMessage(e.ComponentKey.Resource));

        void OnUpdateMessage(ResourceKey resource)
        {
            if (!resource.IsEmpty)
            {
                // Make a note of the entity for updating
                _pendingUpdates.AddDistinct(resource);
            }
        }

        await Task.CompletedTask;

        return Result.Ok();
    }

    public void Uninitialize()
    {
        _messengerService.UnregisterAll(this);
    }

    public async Task<Result> UpdateAsync()
    {
        Guard.IsNotNull(_activityRegistry);

        var initsResult = await PerformPendingInits();
        var updatesResult = await PerformPendingUpdates();

        if (initsResult.IsFailure)
        {
            return initsResult;
        }

        if (updatesResult.IsFailure)
        {
            return updatesResult;
        }

        return Result.Ok();
    }

    private async Task<Result> PerformPendingInits()
    {
        // Perform pending inits
        var pendingInits = _pendingInits.ToList();
        _pendingInits.Clear();

        bool failed = false;
        foreach (var fileResource in pendingInits)
        {
            try
            {
                var initResult = await InitializeResource(fileResource);
                if (initResult.IsFailure)
                {
                    failed = true;
                    _logger.LogError(initResult.Error);
                    continue;
                }
            }
            catch (Exception ex)
            {
                failed = true;
                _logger.LogError(ex.ToString());
            }
        }

        if (failed)
        {
            return Result.Fail($"Failed to perform pending inits");
        }

        return Result.Ok();
    }

    private async Task<Result> PerformPendingUpdates()
    {
        Guard.IsNotNull(_activityRegistry);

        bool failed = false;

        var pendingUpdates = _pendingUpdates.ToList();
        _pendingUpdates.Clear();

        var entityAnnotations = new Dictionary<string, IEntityAnnotation>();

        foreach (var fileResource in pendingUpdates)
        {
            try
            {
                var annotateResult = AnnotateEntity(fileResource);
                if (annotateResult.IsFailure)
                {
                    _logger.LogError(annotateResult.Error);
                    continue;
                }
                var entityAnnotation = annotateResult.Value;

                if (_activityRegistry.Activities.TryGetValue(entityAnnotation.ActivityName, out var activity))
                {
                    // Give the activity an opportunity to update the resource content.
                    var updateResourceResult = await activity.UpdateResourceContentAsync(fileResource, entityAnnotation);
                    if (updateResourceResult.IsFailure)
                    {
                        return Result<IEntityAnnotation>.Fail($"Failed to update entity resource '{fileResource}'")
                            .WithErrors(updateResourceResult);
                    }
                }

                entityAnnotations[fileResource] = entityAnnotation;
            }
            catch (Exception ex)
            {
                failed = true;
                _logger.LogError(ex.ToString());
            }
        }

        // Send a message to the inspector to apply the annotations
        foreach (var kv in entityAnnotations)
        {
            var entity = kv.Key;
            var entityAnnotation = kv.Value;

            var message = new AnnotatedEntityMessage(entity, entityAnnotation);
            _messengerService.Send(message);
        }

        if (failed)
        {
            return Result.Fail($"Failed to perform pending updates");
        }

        return Result.Ok();
    }

    private Result<IEntityAnnotation> AnnotateEntity(ResourceKey fileResource)
    {
        Guard.IsNotNull(_activityRegistry);

        // Get the component list for this entity
        var getComponentsResult = _entityService.GetComponents(fileResource);
        if (getComponentsResult.IsFailure)
        {
            return Result<IEntityAnnotation>.Fail($"Failed to get entity components for resource: '{fileResource}'")
                .WithErrors(getComponentsResult);
        }
        var components = getComponentsResult.Value;

        // Create a new entity annotation
        var entityAnnotation = _serviceProvider.GetRequiredService<IEntityAnnotation>();
        entityAnnotation.Initialize(components.Count);

        if (components.Count == 0)
        {
            // Entity has no components, early out.
            return Result<IEntityAnnotation>.Ok(entityAnnotation);
        }

        bool hasValidRootComponent = true;

        // Get the root component and check that it has a "rootActivity" attribute
        var rootComponent = components[0];
        var activityName = rootComponent.Schema.GetStringAttribute("rootActivity");
        if (string.IsNullOrEmpty(activityName))
        {
            hasValidRootComponent = false;

            entityAnnotation.AddComponentError(0, new EntityError(
                EntityErrorSeverity.Critical,
                "Invalid root component",
                "This component is not a valid root component for this resource."));
        }

        // Perform some basic configuation checks for all components
        for (int i = 0; i < components.Count; i++)
        {
            // Empty components are always valid in any position.
            var component = components[i];
            if (component.Schema.ComponentType == EntityConstants.EmptyComponentType)
            {
                entityAnnotation.SetIsRecognized(i);
            }
            else if (i > 0)
            {
                // Check if this is a root component that's in the wrong position
                var rootActivity = component.Schema.GetStringAttribute("rootActivity");
                if (!string.IsNullOrEmpty(rootActivity))
                {
                    hasValidRootComponent = false;

                    entityAnnotation.AddComponentError(i, new EntityError(
                        EntityErrorSeverity.Critical,
                        "Invalid position",
                        "The root component must be the first component in the list."));
                }
            }
        }

        if (!hasValidRootComponent ||
            activityName == "None")
        {
            // The Empty root component uses the "None" activity to indicate that no activity should be
            // applied to the entity.

            // Flag any non-empty component as an error.
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component.Schema.ComponentType != EntityConstants.EmptyComponentType)
                {
                    entityAnnotation.AddComponentError(i, new EntityError(
                        EntityErrorSeverity.Critical,
                        "Invalid component",
                        "This component is not valid because the root component for this resource is not valid."));
                }
            }
        }

        // Lookup the activity by name
        IActivity? activity = null;
        if (hasValidRootComponent &&
            !_activityRegistry.Activities.TryGetValue(activityName, out activity))
        {
            // An activity name was specified, but the activity doesn't actually exist
            hasValidRootComponent = false;
        }

        if (hasValidRootComponent)
        { 
            entityAnnotation.ActivityName = activityName;
        }
        else
        {
            entityAnnotation.AddEntityError(new EntityError(
                EntityErrorSeverity.Critical,
                "Invalid root component",
                "The root component is not valid for this resource."));

            // We can't do any more annotating of this entity because it does not specify a valid activity.
            return Result<IEntityAnnotation>.Ok(entityAnnotation);
        }

        // Use the activity to finish annotating the entity
        Guard.IsNotNull(activity);
        var updateAnnotationResult = activity.AnnotateEntity(fileResource, entityAnnotation);
        if (updateAnnotationResult.IsFailure)
        {
            return Result<IEntityAnnotation>.Fail($"Failed to update entity annotation for resource '{fileResource}'")
                .WithErrors(updateAnnotationResult);
        }

        return Result<IEntityAnnotation>.Ok(entityAnnotation);
    }

    private async Task<Result> InitializeResource(ResourceKey resource)
    {
        Guard.IsNotNull(_activityRegistry);

        // Search for an activity that supports this resource.
        // Try each activity in alphabetic order for deterministic results.

        var activityNames = _activityRegistry.ActivityNames;
        foreach (var activityName in activityNames)
        {
            var activity = _activityRegistry.Activities[activityName];

            if (activity.SupportsResource(resource))
            {
                // Initialize the resource with this activity
                var initializeResult = await activity.InitializeResourceAsync(resource);
                if (initializeResult.IsFailure)
                {
                    return Result.Fail($"Failed to initialize resource '{resource}' with activity '{activityName}'");
                }
                break;
            }
        }

        // If no activity supports the resource then no initialization is necessary

        return Result.Ok();
    }
}
