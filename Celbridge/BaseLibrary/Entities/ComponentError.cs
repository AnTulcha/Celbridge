namespace Celbridge.Entities;

/// <summary>
/// Error states for misconfigured components.
/// </summary>
public enum ComponentErrorSeverity
{
    /// <summary>
    /// A major configuration issue that should be addressed immediately.
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
/// An error condition for a component.
/// </summary>
public record ComponentError(ComponentErrorSeverity Severity, string Message, string Description);
