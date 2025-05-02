namespace Celbridge.Entities;

/// <summary>
/// Error states for reporting entity and component configuration issues.
/// </summary>
public enum EntityErrorSeverity
{
    /// <summary>
    /// A major issue that should be addressed immediately.
    /// </summary>
    Critical,

    /// <summary>
    /// An issue that prevents the component from functioning.
    /// </summary>
    Error,

    /// <summary>
    /// An issue that may result in unexpected behaviour.
    /// </summary>
    Warning
}

/// <summary>
/// Describes an error condition for a entity or component.
/// </summary>
public record EntityError(EntityErrorSeverity Severity, string Message, string Description);
