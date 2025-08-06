
namespace Celbridge.Console.Services;

public class Terminal : ITerminal
{
#if WINDOWS
    private ConPtyTerminal _terminal = new ConPtyTerminal();
#endif

    public event EventHandler<string>? OutputReceived;

    public Terminal()
    {
#if WINDOWS
        _terminal.OutputReceived += (sender, output) =>
        {
            OutputReceived?.Invoke(sender, output);
        };
#else
        throw new NotImplementedException();
#endif
    }

    public void Start(string commandLine, string workingDir)
    {
#if WINDOWS
        _terminal.Start(commandLine, workingDir);
#else
        throw new NotImplementedException();
#endif
    }

    public void Write(string input)
    {
#if WINDOWS
        _terminal.Write(input);
#else
        throw new NotImplementedException();
#endif
    }
}
