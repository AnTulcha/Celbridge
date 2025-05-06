namespace Celbridge.Entities;

/// <summary>
/// Types of reported information for entity and component state.
/// </summary>
public enum EntityReportType
{
    /// <summary>
    /// Provides information about the state of an entity or component.
    /// </summary>
    Information,

    /// <summary>
    /// An issue that may result in unexpected behaviour.
    /// </summary>
    Warning,

    /// <summary>
    /// An issue that prevents the component from functioning.
    /// </summary>
    Error
}

/// <summary>
/// A report item that describes the status of an entity or component.
/// </summary>
public record EntityReportItem(EntityReportType ReportType, string Message, string Description);
