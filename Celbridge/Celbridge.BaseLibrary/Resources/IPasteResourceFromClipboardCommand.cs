﻿using Celbridge.Commands;

namespace Celbridge.Resources;

/// <summary>
/// Pastes resources from the clipboard.
/// </summary>
public interface IPasteResourceFromClipboardCommand : IExecutableCommand
{
    /// <summary>
    /// Folder resource to paste the clipboard contents into.
    /// </summary>
    ResourceKey DestFolderResource { get; set; }
}