namespace Celbridge.Activities;

/// <summary>
/// Represents a long running activity that runs in the workspace.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// Starts the activity.
    /// </summary>
    /// <returns></returns>
    Task<Result> Start();

    /// <summary>
    /// Stops the activity.
    /// </summary>
    Task<Result> Stop();

    /// <summary>
    /// Updates the activity.
    /// </summary>
    Task<Result> UpdateResourceAsync(ResourceKey resource);

    /// <summary>
    /// Initialize a newly created entity, if the resource is relevant to this activity.
    /// Returns true if the entity was initialized, false otherwise.
    /// </summary>
    bool TryInitializeEntity(ResourceKey resource);
}
