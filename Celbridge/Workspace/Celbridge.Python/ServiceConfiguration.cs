using Celbridge.Commands;
using Celbridge.Python.Commands;
using Celbridge.Python.Services;

namespace Celbridge.Python;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        //
        // Register services
        //

        services.AddTransient<IPythonService, PythonService>();


        //
        // Register views
        //


        //
        // Register view models
        //


        //
        // Register commands
        // 
        services.AddTransient<IExecCommand, ExecCommand>();
    }
}
