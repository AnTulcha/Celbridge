﻿namespace Celbridge.Projects;

/// <summary>
/// Manages all project data for a Celbridge project.
/// </summary>
public interface IProjectData
{
    /// <summary>
    /// Returns the name of the project.
    /// </summary>
    string ProjectName { get; }

    /// <summary>
    /// Returns the path to the loaded project file.
    /// </summary>
    string ProjectFilePath { get; }

    /// <summary>
    /// Returns the path to the folder containing the loaded project.
    /// </summary>
    string ProjectFolderPath { get; }

    /// <summary>
    /// Returns the path to the loaded project database.
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Returns the path to the loaded project's log folder.
    /// </summary>
    string LogFolderPath { get; }

    /// <summary>
    /// Gets the data version for the project data.
    /// </summary>
    Task<Result<int>> GetDataVersionAsync();

    /// <summary>
    /// Sets the data version for the project data.
    /// </summary>
    Task<Result> SetDataVersionAsync(int version);
}