using Celbridge.Logging;
using Celbridge.Projects;
using Celbridge.Workspace;

using Path = System.IO.Path;

namespace Celbridge.Python.Services;

public class PythonService : IPythonService, IDisposable
{
    private readonly ILogger<PythonService> _logger;
    private readonly IProjectService _projectService;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public PythonService(
        ILogger<PythonService> logger,
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _projectService = projectService;
        _workspaceWrapper = workspaceWrapper;
    }

    public async Task<Result> InitializePython()
    {
        if (_projectService.CurrentProject is null)
        {
            return Result<string>.Fail("Failed to run python script as no project is loaded");
        }
        var workingDir = _projectService.CurrentProject.ProjectFolderPath;

        var installResult = await PythonInstaller.InstallPythonAsync();
        if (installResult.IsFailure)
        {
            return Result.Fail("Failed to ensure Python is installed")
                .WithErrors(installResult);
        }
        var pythonFolder = installResult.Value;

        var pythonPath = Path.Combine(pythonFolder, "python.exe");
        pythonPath = GetSafeQuotedPath(pythonPath);

        var scriptPath = Path.Combine(pythonFolder, "startup.py");
        scriptPath = GetSafeQuotedPath(scriptPath);

        // Run startup script then switch to IPython interactive mode
        var commandLine = $"{pythonPath} -m IPython --no-banner -i {scriptPath}";

        var terminal = _workspaceWrapper.WorkspaceService.ConsoleService.Terminal;
        terminal.Start(commandLine, workingDir);

        return Result.Ok();
    }
    private static string GetSafeQuotedPath(string path)
    {
        if (path.Any(char.IsWhiteSpace) &&
           !(path.StartsWith("\"") && path.EndsWith("\"")))
        {
            path = $"\"{path}\"";
        }

        return path;
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed objects here
            }

            _disposed = true;
        }
    }

    ~PythonService()
    {
        Dispose(false);
    }
}
