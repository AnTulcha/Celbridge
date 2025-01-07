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
            var componentSchemas = _entityService.GetComponentSchemaList(fileResource);

            var unprocessedComponents = new List<int>();

            // Annotate empty components

            for (int i = 0; i < componentSchemas.Count; i++)
            {
                var schema = componentSchemas[i];

                if (schema.ComponentType != "Empty")
                {
                    unprocessedComponents.Add(i);
                    continue;
                }

                AnnotateEmptyComponent(fileResource, i);
            }

            // Check that the entity has a Primary Component

            bool hasPrimaryComponent = _entityService.HasTag(fileResource, "PrimaryComponent");
            if (!hasPrimaryComponent)
            {
                // Todo: Annotate the entity and all components with an error message
                foreach (var componentIndex in unprocessedComponents)
                {
                    AnnotateNoPrimaryComponentError(fileResource, componentIndex);
                }

                // Move onto next modified file resource
                continue;
            }

            // Check that there is at least one component.
            // There must be if the PrimaryComponent tag is present.

            var getCountResult = _entityService.GetComponentCount(fileResource);
            if (getCountResult.IsFailure)
            {
                return Result.Fail($"Failed to get component count for entity: '{fileResource}'");
            }
            var componentCount = getCountResult.Value;

            if (componentCount == 0)
            {
                return Result.Fail($"Component count is zero for entity: '{fileResource}'");
            }

            // Get the schema for the Primary Component

            ComponentSchema? primaryComponentSchema = null;

            bool syntaxError = false;

            foreach (var componentIndex in unprocessedComponents)
            {
                var schema = componentSchemas[componentIndex];

                if (schema.ComponentType == "Empty")
                {
                    // Ignore comments
                    continue;
                }

                if (schema.HasTag("PrimaryComponent"))
                {
                    if (primaryComponentSchema is not null)
                    {
                        // Todo: Annotate the component with an error message
                        // Multiple Primary Components detected
                        continue;
                    }

                    // Found the Primary Component
                    primaryComponentSchema = schema;
                }
                else
                {
                    if (primaryComponentSchema is null)
                    {
                        var annotation = new ComponentAnnotation(
                            ComponentStatus.Error, 
                            "Invalid component position", 
                            "This component must be placed after the Primary Component");

                        _entityService.UpdateComponentAnnotation(fileResource, componentIndex, annotation);

                        syntaxError = true;

                        continue;
                    }
                }
            }
        
            if (primaryComponentSchema is null)
            {
                syntaxError = true;
                continue;
            }

            if (syntaxError) 
            {
                // Todo: Annotate all components with an error message
                // Invalid component
                // No Primary Component defined

                continue;
            }

            // Get the activity name for this Primary Component

            var activityName = primaryComponentSchema.GetStringAttribute("activityName");
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

            // Use the activity to annotate all components in the entity

            var updateResult = await activity.UpdateResourceAsync(fileResource);
            if (updateResult.IsFailure)
            {
                return Result.Fail($"Failed to update resource: '{fileResource}'")
                    .WithErrors(updateResult);
            }
        }

        _pendingEntityUpdates.Clear();

        await Task.CompletedTask;

        return Result.Ok();
    }

    private Result AnnotateEmptyComponent(ResourceKey resource, int componentIndex)
    {
        var comment = _entityService.GetString(resource, componentIndex, "/comment");

        var annotation = new ComponentAnnotation(
            ComponentStatus.Valid,
            comment,
            comment);

        return _entityService.UpdateComponentAnnotation(resource, componentIndex, annotation);
    }

    private void AnnotateNoPrimaryComponentError(ResourceKey fileResource, int componentIndex)
    {
        // Todo: Display the activityName property for the component type

        var annotation = new ComponentAnnotation(
            ComponentStatus.Error,
            "No Primary Component",
            $"This component requires Primary Component");

        _entityService.UpdateComponentAnnotation(fileResource, componentIndex, annotation);
    }
}
