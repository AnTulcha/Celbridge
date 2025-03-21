using Celbridge.Commands;
using Celbridge.Workspace;

namespace Celbridge.Documents.Commands;

public class OpenDocumentCommand : CommandBase, IOpenDocumentCommand
{
    public override CommandFlags CommandFlags => CommandFlags.SaveWorkspaceState;

    private readonly IWorkspaceWrapper _workspaceWrapper;

    public ResourceKey FileResource { get; set; }

    public bool ForceReload { get; set; }

    public OpenDocumentCommand(IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var documentsService = _workspaceWrapper.WorkspaceService.DocumentsService;

        var openResult = await documentsService.OpenDocument(FileResource, ForceReload);
        if (openResult.IsFailure)
        {
            return Result.Fail($"Failed to open document for file resource '{FileResource}'")
                .WithErrors(openResult);
        }

        return Result.Ok();
    }

    //
    // Static methods for scripting support.
    //
    public static void OpenDocument(ResourceKey fileResource)
    {
        var commandService = ServiceLocator.AcquireService<ICommandService>();

        commandService.Execute<IOpenDocumentCommand>(command =>
        {
            command.FileResource = fileResource;
        });
    }

    public static void OpenDocument(ResourceKey fileResource, bool forceReload)
    {
        var commandService = ServiceLocator.AcquireService<ICommandService>();

        commandService.Execute<IOpenDocumentCommand>(command =>
        {
            command.FileResource = fileResource;
            command.ForceReload = forceReload;
        });
    }
}
