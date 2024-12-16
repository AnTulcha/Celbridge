namespace Celbridge.Activities;

/// <summary>
/// Provides methods for managing long-running activities in the loaded workspace.
/// </summary>
public interface IActivitiesService
{
    /// <summary>
    /// Initializes the activities service.
    /// </summary>
    Task<Result> Initialize();
}
