using Celbridge.Console.ViewModels;
using Celbridge.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.Web.WebView2.Core;

namespace Celbridge.Console.Views;

public sealed partial class ConsolePanel : UserControl, IConsolePanel
{
    private ILogger<ConsolePanel> _logger;

    public ConsolePanelViewModel ViewModel { get; }

    private ITerminal? _terminal;

    public ConsolePanel()
    {
        this.InitializeComponent();

        _logger = ServiceLocator.AcquireService<ILogger<ConsolePanel>>();
        ViewModel = ServiceLocator.AcquireService<ConsolePanelViewModel>();
    }

    public async Task<Result> ExecuteCommand(string command, bool logCommand)
    {
        await Task.CompletedTask;
        return Result.Ok();
    }

    public async Task<Result> InitializeTerminalWindow(ITerminal terminal)
    {
        _terminal = terminal;

        await TerminalWebView.EnsureCoreWebView2Async();

        var tcs = new TaskCompletionSource<bool>();
        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            TerminalWebView.NavigationCompleted -= Handler;
            tcs.TrySetResult(args.IsSuccess);
        }
        TerminalWebView.NavigationCompleted += Handler;

        TerminalWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("Terminal",
            "Celbridge.Console/Assets/Terminal",
            CoreWebView2HostResourceAccessKind.Allow);
        TerminalWebView.CoreWebView2.Navigate("http://Terminal/index.html");

        bool success = await tcs.Task;

        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

        TerminalWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        _terminal.OutputReceived += (_, output) =>
        {
            dispatcher.TryEnqueue(async () =>
            {
                await SendToTerminalAsync(output);
            });
        };

        return Result.Ok();
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string input = args.TryGetWebMessageAsString();

        if (_terminal is not null &&
            !string.IsNullOrEmpty(input))
        {
            _terminal.Write(input);
        }
    }

    private async Task SendToTerminalAsync(string text)
    {
        TerminalWebView.CoreWebView2.PostWebMessageAsString(text);
        await Task.CompletedTask;
    }
}
