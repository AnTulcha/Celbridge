namespace Celbridge.Python;

/// <summary>
/// A service for interacting with Python via the terminal.
/// </summary>
public interface IPythonService
{
    /// <summary>
    /// Initializes the Python environment.
    /// </summary>
    Task<Result> InitializePython();

    /// <summary>
    /// Runs a Python script via the terminal.
    /// </summary>
    Task<Result<string>> ExecuteAsync(string script);
}
