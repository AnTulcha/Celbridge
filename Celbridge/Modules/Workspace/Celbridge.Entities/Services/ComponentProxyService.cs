using Celbridge.Entities.Models;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Entities.Services;

public class ComponentProxyService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public ComponentProxyService(
        IServiceProvider serviceProvider,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _workspaceWrapper = workspaceWrapper;
    }

    public Result Initialize()
    {
        return Result.Ok();
    }

    // Todo: A proxy contains: a resource, a component index and the component schema.
    // The schema contains the component type, version, info, prototype, descriptor, etc.

    //public Result<IComponentProxy> AcquireComponentProxy(ResourceKey resource, int componentIndex)
    //{
    //    var entityService = _workspaceWrapper.WorkspaceService.EntityService;

    //    // Todo: Check for a cached component

    //    var getInfoResult = entityService.GetComponentInfo(resource, componentIndex);
    //    if (getInfoResult.IsFailure)
    //    {
    //        return Result<IComponentProxy>.Fail($"Failed to get component info for component at index '{componentIndex}' for resource '{resource}'")
    //            .WithErrors(getInfoResult);
    //    }
    //    var componentInfo = getInfoResult.Value;
    //    var componentType = componentInfo.ComponentType;

    //    var componentInstance = _serviceProvider.GetRequiredService(objectType) as IComponentDescriptor;
    //    Guard.IsNotNull(componentInstance);

    //    // Wrap the component instance in a ComponentProxy

    //    var proxy = new ComponentProxy(_serviceProvider, resource, componentIndex, componentInfo, componentInstance);

    //    return Result<IComponentProxy>.Ok(proxy);
    //}
}
