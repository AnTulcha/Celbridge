using Celbridge.Commands;
using Celbridge.Workspace;

namespace Celbridge.Entities.Commands;

public class SetPropertyCommand : CommandBase, ISetPropertyCommand
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public ComponentKey ComponentKey { get; set; } = ComponentKey.Empty;
    public string PropertyPath { get; set; } = string.Empty;
    public string JsonValue { get; set; } = string.Empty;
    public bool Insert { get; set; }

    public SetPropertyCommand(
        IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var entityService = _workspaceWrapper.WorkspaceService.EntityService;

        var setResult = entityService.SetPropertyAsJson(ComponentKey, PropertyPath, JsonValue, Insert);

        await Task.CompletedTask;

        return setResult;
    }

    //
    // Static methods for scripting support.
    //

    /// <summary>
    /// Replaces an existing component property value at the specified path.
    /// If the property value does not exist, the operation fails.
    /// </summary>
    public static async Task<Result> SetProperty(ResourceKey resource, int componentIndex, string propertyPath, string jsonValue)
    {
        var commandService = ServiceLocator.AcquireService<ICommandService>();

        return await commandService.ExecuteAsync<ISetPropertyCommand>(command =>
        {
            command.ComponentKey = new ComponentKey(resource, componentIndex);
            command.PropertyPath = propertyPath;
            command.JsonValue = jsonValue;
            command.Insert = false;
        });
    }

    /// <summary>
    /// Inserts a new component property value at the specified path.
    /// If the property value already exists, it is replaced.
    /// </summary>
    public static async Task<Result> InsertProperty(ResourceKey resource, int componentIndex, string propertyPath, string jsonValue)
    {
        var commandService = ServiceLocator.AcquireService<ICommandService>();

        return await commandService.ExecuteAsync<ISetPropertyCommand>(command =>
        {
            command.ComponentKey = new ComponentKey(resource, componentIndex);
            command.PropertyPath = propertyPath;
            command.JsonValue = jsonValue;
            command.Insert = true;
        });
    }
}
