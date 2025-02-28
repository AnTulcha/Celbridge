﻿namespace Celbridge.Documents;

/// <summary>
/// The supported types of document view.
/// </summary>
public enum DocumentViewType
{
    /// <summary>
    /// An unsupported document format.
    /// The resource has an unrecognized file extension.
    /// </summary>
    UnsupportedFormat,

    /// <summary>
    /// Text document edited using a text editor control.
    /// e.g. .txt, .cs, .json, .xml, etc.
    /// </summary>
    TextDocument,

    /// <summary>
    /// A web page document with the .web extension.
    /// </summary>
    WebPageDocument,

    /// <summary>
    /// A non-text file resource viewed via a web view.
    /// e.g. an image, audio clip, pdf file, etc.
    /// </summary>
    FileViewer
}
