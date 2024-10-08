namespace Celbridge.Console;

/// <summary>
/// The type of message to print to the console.
/// </summary>
public enum MessageType
{
    Command,
    Information,
    Warning,
    Error,
}

/// <summary>
/// The console service provides functionality to support the console panel in the workspace UI.
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// Returns the console panel view.
    /// </summary>
    IConsolePanel ConsolePanel { get; }

    /// <summary>
    /// Event fired when the Print() method is called.
    /// </summary>
    event Action<MessageType, string> OnPrint;

    /// <summary>
    /// Print a message to the console.
    /// </summary>
    void Print(MessageType messageType, string message);
}
