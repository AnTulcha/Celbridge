using Celbridge.Commands;

namespace Celbridge.Python;

public interface IExecCommand : IExecutableCommand
{
    /// <summary>
    /// Python script to execute.
    /// </summary>
    public string Script { get; set; }
}
