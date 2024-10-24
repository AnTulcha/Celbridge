using Celbridge.Extensions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Celbridge.Extensions;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        //
        // Register services
        //
        services.AddSingleton<IExtensionService, ExtensionService>();
        services.AddTransient<IExtensionContext, ExtensionContext>();
    }

    public static void Initialize()
    {}
}
