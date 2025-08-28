using Celbridge.Projects;
using Celbridge.Utilities;
using Celbridge.Workspace;

using Path = System.IO.Path;

namespace Celbridge.Python.Services;

public class PythonService : IPythonService, IDisposable
{
    private readonly IProjectService _projectService;
    private readonly IWorkspaceWrapper _workspaceWrapper;
    private readonly IUtilityService _utilityService;

    public PythonService(
        IProjectService projectService,
        IWorkspaceWrapper workspaceWrapper,
        IUtilityService utilityService)
    {
        _projectService = projectService;
        _workspaceWrapper = workspaceWrapper;
        _utilityService = utilityService;
    }

    public async Task<Result> InitializePython()
    {
        try
        {
            var project = _projectService.CurrentProject;
            if (project is null)
                return Result.Fail("Failed to run python script as no project is loaded");

            var workingDir = project.ProjectFolderPath;

            var installResult = await PythonInstaller.InstallPythonAsync();
            if (installResult.IsFailure)
                return Result.Fail("Failed to ensure Python is installed").WithErrors(installResult);

            var toolsFolder = installResult.Value;

            // uv path (handles Windows/macOS/Linux)
            var uvFileName = OperatingSystem.IsWindows() ? "uv.exe" : "uv";
            var uvExePath = Path.Combine(toolsFolder, uvFileName);
            if (!File.Exists(uvExePath))
                return Result.Fail($"uv not found at '{uvExePath}'");

            // startup script
            var scriptPath = Path.Combine(toolsFolder, "startup.py");
            if (!File.Exists(scriptPath))
                return Result.Fail($"startup.py not found at '{scriptPath}'");

            // IPython working dir
            var ipyDir = Path.Combine(workingDir, ".celbridge", "ipython");
            Directory.CreateDirectory(ipyDir);

            SetCelbridgeVersion();

            var pythonConfig = project.ProjectConfig?.Config?.Python!;
            if (pythonConfig is null)
            {
                return Result.Fail("Python section not specified in project config");
            }

            var pythonVersion = pythonConfig.Version;
            if (string.IsNullOrWhiteSpace(pythonVersion))
            {
                return Result.Fail("Python version not specified");
            }

            var packageArgs = new List<string>()
            {
                "--with", "ipython"
            };

            var pythonPackages = pythonConfig.Packages;
            if (pythonPackages is not null)
            {
                foreach (var pythonPackage in pythonPackages)
                {
                    packageArgs.Add("--with");
                    packageArgs.Add(pythonPackage);    
                }
            }

            var commandLine = new CommandLineBuilder(uvExePath)
                .Add("run")
                .Add("--python", pythonVersion!)
                .Add(packageArgs.ToArray())
                .Add("python", "-m", "IPython")
                .Add("--no-banner")
                .Add("--ipython-dir", ipyDir)
                .Add("-i", scriptPath)
                .ToString();

            var terminal = _workspaceWrapper.WorkspaceService.ConsoleService.Terminal;
            terminal.Start(commandLine, workingDir);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail("An error occurred when initializing Python")
                         .WithException(ex);
        }
    }

    private static string BuildCommandLine(string exePath, IEnumerable<string> args)
    {
        var parts = new List<string> { QuoteArg(exePath) };
        parts.AddRange(args.Select(QuoteArg));
        return string.Join(" ", parts);
    }

    private static string QuoteArg(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return OperatingSystem.IsWindows() ? "\"\"" : "''";

        if (OperatingSystem.IsWindows())
        {
            // Needs quoting if it contains whitespace or quotes
            bool needQuotes = arg.Any(ch => ch == ' ' || ch == '\t' || ch == '\n' || ch == '\v' || ch == '"');
            if (!needQuotes) return arg;

            var sb = new System.Text.StringBuilder();
            sb.Append('"');
            int backslashes = 0;
            foreach (char c in arg)
            {
                if (c == '\\')
                {
                    backslashes++;
                }
                else if (c == '"')
                {
                    // Escape all backslashes + the quote
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                }
                else
                {
                    if (backslashes > 0)
                    {
                        sb.Append('\\', backslashes);
                        backslashes = 0;
                    }
                    sb.Append(c);
                }
            }
            // Escape trailing backslashes
            if (backslashes > 0) sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }
        else
        {
            // POSIX: single-quote, escape embedded single quotes as: ' foo'\'bar '
            return "'" + arg.Replace("'", "'\"'\"'") + "'";
        }
    }

    private void SetCelbridgeVersion()
    {
        // Set the Celbridge version number as an environment variable so we can print it from Python at startup.
        var environmentInfo = _utilityService.GetEnvironmentInfo();
        var version = environmentInfo.AppVersion;
        var configuration = environmentInfo.Configuration;
        var celbridgeVersion = configuration == "Debug" ? $"{version} (Debug)" : $"{version}";
        Environment.SetEnvironmentVariable("CELBRIDGE_VERSION", $"{celbridgeVersion}");
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
