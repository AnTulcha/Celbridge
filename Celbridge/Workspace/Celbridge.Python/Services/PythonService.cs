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
        var psi = new ProcessStartInfo
        {
            FileName = "py",
            Arguments = $"-c \"{script}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = psi
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) => {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) => {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var errors = errorBuilder.ToString();

        return !string.IsNullOrWhiteSpace(errors) ? $"ERROR:\n{errors}" : output;
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
