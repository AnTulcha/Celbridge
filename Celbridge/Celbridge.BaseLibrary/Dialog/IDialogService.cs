﻿using Celbridge.BaseLibrary.Project;

namespace Celbridge.BaseLibrary.Dialog;

/// <summary>
/// Manages the display of modal dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Display an Alert Dialog with configurable title, message and close button text.
    /// </summary>
    Task ShowAlertDialogAsync(string titleText, string messageText, string closeText);

    /// <summary>
    /// Aqcuire a progress dialog token.
    /// The progress dialog will be displayed as long as any token is active, and will display the title of the
    /// most recently acquired token that is still active. The progress dialog is temporarily hidden while any other type 
    /// of dialog is displayed.
    /// </summary>
    IProgressDialogToken AcquireProgressDialog(string titleText);

    /// <summary>
    /// Release a previously acquired progress dialog token.
    /// The progress dialog is hidden when all tokens are released.
    /// </summary>
    void ReleaseProgressDialog(IProgressDialogToken token);

    /// <summary>
    /// Display a New Project Dialog.
    /// </summary>
    Task<Result<NewProjectConfig>> ShowNewProjectDialogAsync();
}