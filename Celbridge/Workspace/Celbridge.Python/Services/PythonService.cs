using Celbridge.Logging;

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
        _logger.LogInformation(script);

        await Task.CompletedTask;
            
        return Result<string>.Ok(string.Empty);
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
