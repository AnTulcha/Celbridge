using Celbridge.Commands;
using Celbridge.Core;
using Celbridge.Workspace;

namespace Celbridge.Entities.Commands;

public class RedoEntityCommand : CommandBase, IRedoEntityCommand
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public ResourceKey Resource { get; set; }

    public RedoEntityCommand(
        IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var entityService = _workspaceWrapper.WorkspaceService.EntityService;

        var redoResult = entityService.TryRedoEntity(Resource);

        await Task.CompletedTask;

        return redoResult;
    }

    //
    // Static methods for scripting support.
    //

    public static void RedoEntity(ResourceKey resource)
    {
        var commandService = ServiceLocator.ServiceProvider.GetRequiredService<ICommandService>();
        commandService.Execute<IRedoEntityCommand>(command =>
        {
            command.Resource = resource;
        });
    }
}
