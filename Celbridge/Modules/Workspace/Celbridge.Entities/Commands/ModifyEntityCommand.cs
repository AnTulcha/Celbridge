using System.Text.Json;
using Celbridge.Commands;
using Celbridge.Core;
using Celbridge.Workspace;

namespace Celbridge.Entities.Commands;

public class ModifyEntityCommand : CommandBase, IModifyEntityCommand
{
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public ResourceKey Resource { get; set; }
    public string Patch { get; set; } = string.Empty;

    public ModifyEntityCommand(
        IWorkspaceWrapper workspaceWrapper)
    {
        _workspaceWrapper = workspaceWrapper;
    }

    public override async Task<Result> ExecuteAsync()
    {
        var entityService = _workspaceWrapper.WorkspaceService.EntityService;

        var applyResult = entityService.ApplyPatch(Resource, Patch);

        await Task.CompletedTask;

        return applyResult;
    }

    //
    // Static methods for scripting support.
    //

    private record AddOperation(string op, string path, object value);
    public static void AddProperty(ResourceKey resource, string path, object value)
    {
        var operation = new AddOperation("add", path, value);
        string patch = JsonSerializer.Serialize(operation);
        patch = $"[{patch}]";
        ApplyPatch(resource, patch);
    }

    public static void AddProperty(ResourceKey resource, int componentIndex, string path, object value)
    {
        var componentPath = $"/_components/{componentIndex}/{path}";
        AddProperty(resource, componentPath, value);
    }

    private record RemoveOperation(string op, string path);
    public static void RemoveProperty(ResourceKey resource, string path)
    {
        var operation = new RemoveOperation("remove", path);
        string patch = JsonSerializer.Serialize(operation);
        patch = $"[{patch}]";
        ApplyPatch(resource, patch);
    }

    public static void RemoveProperty(ResourceKey resource, int componentIndex, string path)
    {
        var componentPath = $"/_components/{componentIndex}/{path}";
        RemoveProperty(resource, componentPath);
    }

    private record ReplaceOperation(string op, string path, object value);
    public static void ReplaceProperty(ResourceKey resource, string path, object value)
    {
        var operation = new ReplaceOperation("replace", path, value);
        string patch = JsonSerializer.Serialize(operation);
        patch = $"[{patch}]";
        ApplyPatch(resource, patch);
    }

    public static void ReplaceProperty(ResourceKey resource, int componentIndex, string path, object value)
    {
        var componentPath = $"/_components/{componentIndex}/{path}";
        RemoveProperty(resource, componentPath);
    }

    private record MoveOperation(string op, string from, string path);
    public static void MoveProperty(ResourceKey resource, string sourcePath, string destPath)
    {
        var operation = new CopyOperation("move", sourcePath, destPath);
        string patch = JsonSerializer.Serialize(operation);
        patch = $"[{patch}]";
        ApplyPatch(resource, patch);
    }

    public static void MoveComponent(ResourceKey resource, int sourceComponentIndex, int destComponentIndex)
    {
        var sourceComponentPath = $"/_components/{sourceComponentIndex}";
        var destComponentPath = $"/_components/{destComponentIndex}";
        MoveProperty(resource, sourceComponentPath, destComponentPath);
    }

    private record CopyOperation(string op, string from, string path);
    public static void CopyProperty(ResourceKey resource, string sourcePath, string destPath)
    {
        var operation = new CopyOperation("copy", sourcePath, destPath);
        string patch = JsonSerializer.Serialize(operation);
        patch = $"[{patch}]";
        ApplyPatch(resource, patch);
    }

    public static void CopyComponent(ResourceKey resource, int sourceComponentIndex, int destComponentIndex)
    {
        var sourceComponentPath = $"/_components/{sourceComponentIndex}";
        var destComponentPath = $"/_components/{destComponentIndex}";
        CopyProperty(resource, sourceComponentPath, destComponentPath);
    }

    public static void ApplyPatch(ResourceKey resource, string patch)
    {
        var commandService = ServiceLocator.ServiceProvider.GetRequiredService<ICommandService>();
        commandService.Execute<IModifyEntityCommand>(command =>
        {
            command.Resource = resource;
            command.Patch = patch;
        });
    }
}
