﻿using Celbridge.BaseLibrary.Commands;
using Celbridge.BaseLibrary.Resources;

namespace Celbridge.BaseLibrary.Project;

/// <summary>
/// Add a file or folder resource to the project.
/// </summary>
public interface IAddResourceCommand : IExecutableCommand
{
    /// <summary>
    /// The type of resource to add
    /// </summary>
    ResourceType ResourceType { get; set; }

    /// <summary>
    /// Resource key for the new resource
    /// </summary>
    ResourceKey ResourceKey { get; set; }

    /// <summary>
    /// Path to copy the resource from.
    /// If empty, then an empty resource is created.
    /// </summary>
    string SourcePath { get; set; }
}