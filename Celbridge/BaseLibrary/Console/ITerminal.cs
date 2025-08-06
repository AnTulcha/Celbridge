namespace Celbridge.Console;

/// <summary>
/// A terminal window instance used to interact with command line programs.
/// </summary>
public interface ITerminal
{
    /// <summary>
    /// Starts the terminal session by executing a command line program.
    /// </summary>
    void Start(string commandLine, string workingDir);

    /// <summary>
    /// Writes input data to the terminal.
    /// </summary>
    void Write(string input);

    /// <summary>
    /// Event fired when the terminal has received output data.
    /// </summary>
    event EventHandler<string>? OutputReceived;

}
