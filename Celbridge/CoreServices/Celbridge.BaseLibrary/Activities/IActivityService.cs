namespace Celbridge.Activities;

/// <summary>
/// Provides methods for managing long-running activities in the loaded workspace.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Initializes the activity service.
    /// </summary>
    Task<Result> Initialize();

    /// <summary>
    /// Updates the activities in the workspace.
    /// </summary>
    Task<Result> UpdateActivitiesAsync();
}
