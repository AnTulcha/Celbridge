using Celbridge.Logging;
using System.Diagnostics;

namespace Celbridge.Python.Services;

public class PythonService : IPythonService, IDisposable
{
    private readonly ILogger<PythonService> _logger;

    public PythonService(ILogger<PythonService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<string>> ExecuteAsync(string script)
    {
        var result = await RunPythonAsync(script);

        _logger.LogInformation(result);

        await Task.CompletedTask;
            
        return Result<string>.Ok(string.Empty);
    }

    private async Task<string> RunPythonAsync(string script)
    {
        var result = await PythonRuntime.RunScriptAsync(script);

        return result;
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
