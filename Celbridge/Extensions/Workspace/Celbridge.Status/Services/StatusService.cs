﻿using Celbridge.Status.Views;

namespace Celbridge.Status.Services;

public class StatusService : IStatusService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public StatusService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IStatusPanel CreateStatusPanel()
    {
        return _serviceProvider.GetRequiredService<IStatusPanel>();
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

    ~StatusService()
    {
        Dispose(false);
    }
}