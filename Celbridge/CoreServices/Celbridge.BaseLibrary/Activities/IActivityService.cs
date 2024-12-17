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
    /// Requests an inspector update on the inspected entity from the specified activity.
    /// </summary>
    Result RequestInpectorUpdate(string activityName);

    /// <summary>
    /// Updates the activities in the workspace.
    /// </summary>
    Task<Result> UpdateActivities();
}
