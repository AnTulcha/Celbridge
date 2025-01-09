using Celbridge.Entities.Models;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Entities.Services;

public class ComponentProxyService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;
    private readonly IWorkspaceWrapper _workspaceWrapper;
    private IEntityService? _entityService;

    private Dictionary<ResourceKey, Dictionary<int, ComponentProxy>> _proxyCache = new();
    private Dictionary<ResourceKey, IReadOnlyList<IComponentProxy>> _proxyListCache = new();

    public ComponentProxyService(
        IServiceProvider serviceProvider,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _messengerService = messengerService;
        _workspaceWrapper = workspaceWrapper;
    }

    public Result Initialize()
    {
        _entityService = _workspaceWrapper.WorkspaceService.EntityService;

        _messengerService.Register<ComponentChangedMessage>(this, OnComponentChangedMessage);

        return Result.Ok();
    }

    public Result Uninitialize()
    {
        _messengerService.UnregisterAll(this);

        return Result.Ok();
    }

    private void OnComponentChangedMessage(object recipient, ComponentChangedMessage message)
    {
        var resource = message.Resource;
        var propertyPath = message.PropertyPath;

        if (propertyPath == "/")
        {
            if (_proxyCache.TryGetValue(resource, out var indexCache))
            {
                // Invalidate all proxies in the index cache before removing them.
                // If a client is holding a reference to a proxy, they can check if it is valid before using it.
                foreach (var proxy in indexCache.Values)
                {
                    proxy.IsValid = false;
                }
            }
            _proxyCache.Remove(resource);
            _proxyListCache.Remove(resource);
        }
    }

    public Result<IComponentProxy> GetComponent(ResourceKey resource, int componentIndex)
    {
        Guard.IsNotNull(_entityService);

        // Attempt to get the proxy from the cache
        if (!_proxyCache.TryGetValue(resource, out var indexCache))
        {
            indexCache = new Dictionary<int, ComponentProxy>();
            _proxyCache[resource] = indexCache;
        }
        if (indexCache.TryGetValue(componentIndex, out var cachedProxy))
        {
            return Result<IComponentProxy>.Ok(cachedProxy);
        }

        // No proxy found in the cache, create a new one

        // Get the component type
        var getTypeResult = _entityService.GetComponentType(resource, componentIndex);
        if (getTypeResult.IsFailure)
        {
            return Result<IComponentProxy>.Fail($"Failed to get component type for resource '{resource}' at component index {componentIndex}")
                .WithErrors(getTypeResult);
        }
        var componentType = getTypeResult.Value;

        // Get the component schema
        var getSchemaResult = _entityService.GetComponentSchema(componentType);
        if (getSchemaResult.IsFailure)
        {
            return Result<IComponentProxy>.Fail($"Failed to get component schema for component type '{componentType}'")
            .WithErrors(getSchemaResult);
        }
        var schema = getSchemaResult.Value;

        var proxy = new ComponentProxy(_serviceProvider, resource, componentIndex, schema);
        indexCache[componentIndex] = proxy;

        return Result<IComponentProxy>.Ok(proxy);
    }

    public Result<IReadOnlyList<IComponentProxy>> GetComponents(ResourceKey resource)
    {
        Guard.IsNotNull(_entityService);

        if (_proxyListCache.TryGetValue(resource, out var list))
        {
            return Result<IReadOnlyList<IComponentProxy>>.Ok(list);
        }

        // Get the component count
        var getCountResult = _entityService.GetComponentCount(resource);
        if (getCountResult.IsFailure)
        {
            return Result<IReadOnlyList<IComponentProxy>>.Fail($"Failed to get component count for resource '{resource}'")
                .WithErrors(getCountResult);
        }
        var count = getCountResult.Value;

        // Populate the proxy list
        var proxies = new List<IComponentProxy>(count);
        for (int i = 0; i < count; i++)
        {
            var getComponentResult = GetComponent(resource, i);
            if (getCountResult.IsFailure)
            {
                return Result<IReadOnlyList<IComponentProxy>>.Fail($"Failed to get component for resource '{resource}' at index {i}")
                    .WithErrors(getComponentResult);
            }
            var component = getComponentResult.Value;

            proxies.Add(component);
        }
        _proxyListCache[resource] = proxies;

        return Result<IReadOnlyList<IComponentProxy>>.Ok(proxies);
    }
}
