namespace Celbridge.Activities;

/// <summary>
/// Represents a long running activity that can be performed in the workspace.
/// </summary>
public interface IActivity
{
    /// <summary>
    /// Returns the name of the activity.
    /// </summary>
    string ActivityName { get; }

    /// <summary>
    /// Updates the activity.
    /// </summary>
    Task<Result> UpdateAsync();
}
