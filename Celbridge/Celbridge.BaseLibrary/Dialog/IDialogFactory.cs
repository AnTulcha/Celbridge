﻿namespace Celbridge.BaseLibrary.Dialog;

/// <summary>
/// Provides factory methods for creating various types of modal dialogs.
/// </summary>
public interface IDialogFactory
{
    /// <summary>
    /// Create an Alert Dialog with configurable title, message and close button text.
    /// </summary>
    IAlertDialog CreateAlertDialog(string titleText, string messageText, string closeText);

    /// <summary>
    /// Create an Progress Dialog.
    /// </summary>
    IProgressDialog CreateProgressDialog();

    /// <summary>
    /// Create an New Project Dialog.
    /// </summary>
    INewProjectDialog CreateNewProjectDialog();
}