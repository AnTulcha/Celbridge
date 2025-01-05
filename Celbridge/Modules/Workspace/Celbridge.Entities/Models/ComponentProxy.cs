using Celbridge.Workspace;

namespace Celbridge.Entities.Models;

public class ComponentProxy : IComponentProxy
{
    private IEntityService _entityService;

    public ResourceKey Resource { get; }

    public int ComponentIndex { get; }

    public ComponentInfo ComponentInfo { get; }

    public IComponentDescriptor ComponentInstance { get; }

    // Todo: Use a single ComponentSchema instead of separate ComponentInfo, IComponentDescriptor, etc.
    public ComponentProxy(IServiceProvider serviceProvider, ResourceKey resource, int componentIndex, ComponentInfo componentInfo, IComponentDescriptor componentDescriptor)
    {
        var workspaceWraper = serviceProvider.GetRequiredService<IWorkspaceWrapper>();
        _entityService = workspaceWraper.WorkspaceService.EntityService;

        Resource = resource;
        ComponentIndex = componentIndex;
        ComponentInfo = componentInfo;
        ComponentInstance = componentDescriptor;
    }

    // Annotation info

    public ComponentStatus Status { get; set; }

    public string Tooltip { get; set; } = string.Empty;

    public virtual string GetComponentDescription()
    {
        throw new NotImplementedException();
    }

    public virtual string GetComponentTooltip()
    {
        throw new NotImplementedException();
    }

    public virtual object GetComponentForm()
    {
        throw new NotImplementedException();
    }

    public string GetString(string propertyPath, string defaultValue = "")
    {
        return _entityService.GetString(Resource, ComponentIndex, propertyPath, defaultValue);
    }
}
