using Celbridge.Logging;

namespace Celbridge.Activities.Services;

public class ActivitiesService : IActivitiesService, IDisposable
{
    private ILogger<ActivitiesService> _logger;

    public ActivitiesService(ILogger<ActivitiesService> logger)
    {
        _logger = logger;
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

    ~ActivitiesService()
    {
        Dispose(false);
    }
}
