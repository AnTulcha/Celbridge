using Celbridge.Modules;
using Celbridge.Entities.Services;
using Celbridge.Entities.Models;

namespace Celbridge.Entities;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //
        config.AddTransient<IEntityService, EntityService>();
        config.AddTransient<EntitySchemaService>();
        config.AddTransient<EntityPrototypeService>();

        //
        // Register models
        //
        config.AddTransient<Entity>();
    }
}
