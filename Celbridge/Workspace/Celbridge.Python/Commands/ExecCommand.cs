using Celbridge.Commands;
using Celbridge.Workspace;

namespace Celbridge.Python.Commands;

public class ExecCommand : CommandBase, IExecCommand
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public string Script { get; set; } = string.Empty;

    public ExecCommand(IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public override async Task<Result> ExecuteAsync()
    {
        if (!_workspaceWrapper.IsWorkspacePageLoaded)
        {
            return Result.Fail("Workspace not loaded");
        }

        var pythonService = _workspaceWrapper.WorkspaceService.PythonService;

        var execResult = await pythonService.ExecuteAsync(Script);
        if (execResult.IsFailure)
        {
            return Result.Fail("Failed to execute Python script")
                .WithErrors(execResult);
        }

        return Result.Ok();
    }

    //
    // Static methods for scripting support.
    //

    public static void Exec(string script)
    {
        var commandService = ServiceLocator.AcquireService<ICommandService>();
        commandService.Execute<IExecCommand>(command => { 
            command.Script = script;
        });
    }
}
