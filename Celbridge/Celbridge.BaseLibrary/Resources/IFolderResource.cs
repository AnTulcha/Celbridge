﻿using System.Collections.ObjectModel;

namespace Celbridge.BaseLibrary.Resources;

/// <summary>
/// A folder resource in the project folder.
/// </summary>
public interface IFolderResource : IResource
{
    ObservableCollection<IResource> Children { get; set; }
}