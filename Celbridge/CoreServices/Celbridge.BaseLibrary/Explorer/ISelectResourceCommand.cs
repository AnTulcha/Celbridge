﻿using Celbridge.Commands;

namespace Celbridge.Explorer;

/// <summary>
/// Selects a resource in the explorer panel.
/// </summary>
public interface ISelectResourceCommand : IExecutableCommand
{
    /// <summary>
    /// The resource to select.
    /// </summary>
    ResourceKey Resource { get; set; }
}