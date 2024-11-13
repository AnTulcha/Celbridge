namespace Celbridge.Entities.Services;

public class EntityService : IEntityService, IDisposable
{
    private const string SchemaPackageFolder = "Schemas";
    private const string PrototypePackageFolder = "Prototypes";

    private EntitySchemaService _schemaService;
    private EntityPrototypeService _prototypeService;

    public EntityService(IServiceProvider serviceProvider)
    {
        _schemaService = serviceProvider.GetRequiredService<EntitySchemaService>();
        _prototypeService = serviceProvider.GetRequiredService<EntityPrototypeService>();
    }

    public async Task<Result> InitializeAsync()
    {
        var loadSchemasResult = await LoadSchemasAsync();
        if (loadSchemasResult.IsFailure)
        {
            return loadSchemasResult;
        }

        var loadPrototypesResult = await LoadPrototypesAsync();
        if (loadPrototypesResult.IsFailure)
        {
            return loadPrototypesResult;
        }

        return Result.Ok();
    }

    public IEntity AcquireEntity(ResourceKey resourceKey)
    {
        throw new NotImplementedException();
    }

    public void DestroyEntity(ResourceKey resourceKey)
    {
        throw new NotImplementedException();
    }

    public void MarkEntityModified(ResourceKey resourceKey)
    {
        throw new NotImplementedException();
    }

    public Result RemapResourceKey(ResourceKey oldResource, ResourceKey newResource)
    {
        throw new NotImplementedException();
    }

    public Task<Result> SavePendingAsync()
    {
        throw new NotImplementedException();
    }

    private async Task<Result> LoadSchemasAsync()
    {
        try
        {
            List<string> jsonContents = new List<string>();

            // The Uno docs only discuss using StorageFile.GetFileFromApplicationUriAsync()
            // to load files from the app package, but Package.Current.InstalledLocation appears
            // to work fine on both Windows and Skia+Gtk platforms.
            // https://platform.uno/docs/articles/features/file-management.html#support-for-storagefilegetfilefromapplicationuriasync
            var schemasFolder = await Package.Current.InstalledLocation.GetFolderAsync(SchemaPackageFolder);

            var jsonFiles = await schemasFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var content = await FileIO.ReadTextAsync(jsonFile);

                _schemaService.AddSchema(content);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading schemas")
                .WithException(ex);
        }
    }

    private async Task<Result> LoadPrototypesAsync()
    {
        try
        {
            List<string> jsonContents = new List<string>();

            var prototypesFolder = await Package.Current.InstalledLocation.GetFolderAsync(PrototypePackageFolder);

            var jsonFiles = await prototypesFolder.GetFilesAsync();
            foreach (var jsonFile in jsonFiles)
            {
                var json = await FileIO.ReadTextAsync(jsonFile);

                var getResult = _schemaService.GetSchemaFromJson(json);
                if (getResult.IsFailure)
                {
                    return Result.Fail($"Failed to get schema for prototype: {jsonFile.DisplayName}")
                        .WithErrors(getResult);
                }

                var schema = getResult.Value;

                var validateResult = schema.ValidateJson(json);
                if (validateResult.IsFailure)
                {
                    return Result.Fail($"Failed to validate prototype")
                        .WithErrors(validateResult);
                }

                _prototypeService.AddPrototype(json);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occurred when loading prototypes")
                .WithException(ex);
        }
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

    ~EntityService()
    {
        Dispose(false);
    }
}
