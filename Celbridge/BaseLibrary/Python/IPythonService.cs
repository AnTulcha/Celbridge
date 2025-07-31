namespace Celbridge.Python;

public interface IPythonService
{
    Task<Result<string>> ExecuteAsync(string script);
}
