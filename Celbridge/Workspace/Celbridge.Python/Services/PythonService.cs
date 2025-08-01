using Celbridge.Logging;
using Celbridge.Projects;

namespace Celbridge.Python.Services;

public class PythonService : IPythonService, IDisposable
{
    private readonly ILogger<PythonService> _logger;
    private readonly IProjectService _projectService;

    public PythonService(
        ILogger<PythonService> logger,
        IProjectService projectService)
    {
        _logger = logger;
        _projectService = projectService;
    }

    public async Task<Result<string>> ExecuteAsync(string script)
    {
        var runResult = await RunPythonAsync(script);

        if (runResult.IsFailure)
        {
            return Result<string>.Fail("Failed to run Python script")
                .WithErrors(runResult);
        }

        var output = runResult.Value;

        _logger.LogInformation(output);

        await Task.CompletedTask;
            
        return Result<string>.Ok(string.Empty);
    }

    private async Task<Result<string>> RunPythonAsync(ResourceKey scriptResource)
    {
        if (_projectService.CurrentProject is null)
        {
            return Result<string>.Fail("Failed to run python script as no project is loaded");
        }

        // Todo: Check scriptResource exists in project

        var projectFolderPath = _projectService.CurrentProject.ProjectFolderPath;
        var runResult = await PythonRuntime.RunScriptAsync(scriptResource, projectFolderPath);

        if (runResult.IsFailure)
        {
            return runResult;
        }
        var output = runResult.Value;

        // Todo: Use a record to return detailed result data
        return Result<string>.Ok(output);
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
