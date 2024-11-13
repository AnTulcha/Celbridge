using Celbridge.Modules;
using Celbridge.Entities.Services;

namespace Celbridge.Entities;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<IEntityService, EntityService>();
        config.AddTransient<IResourceDataService, ResourceDataService>();
        config.AddTransient<EntitySchemaService>();
        config.AddTransient<EntityPrototypeService>();
    }
}
