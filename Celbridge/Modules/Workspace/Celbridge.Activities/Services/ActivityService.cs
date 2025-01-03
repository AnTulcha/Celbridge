using Celbridge.Logging;

namespace Celbridge.Activities.Services;

public class ActivityService : IActivityService, IDisposable
{
    private IServiceProvider _serviceProvider;
    private ILogger<ActivityService> _logger;

    private ActivityRegistry? _activityRegistry;

    public ActivityService(
        IServiceProvider serviceProvider,
        ILogger<ActivityService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Result> Initialize()
    {
        _activityRegistry = _serviceProvider.GetRequiredService<ActivityRegistry>();

        return await _activityRegistry.Initialize();
    }

    public async Task<Result> UpdateActivitiesAsync()
    {
        if (_activityRegistry is null)
        {
            // noop
            return Result.Ok();
        }

        try
        {
            foreach (var kv in _activityRegistry.Activities)
            {
                var activity = kv.Value;

                var updateResult = await activity.UpdateAsync();
                if (updateResult.IsFailure)
                {
                    return Result.Fail($"Failed to update activity '{activity.GetType()}'")
                        .WithErrors(updateResult);
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred while updating activities")
                .WithException(ex);
        }

        return Result.Ok();
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

                _activityRegistry?.Uninitialize();
            }

            _disposed = true;
        }
    }

    ~ActivityService()
    {
        Dispose(false);
    }
}
