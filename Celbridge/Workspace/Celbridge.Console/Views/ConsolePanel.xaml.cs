using Celbridge.Console.ViewModels;
using Celbridge.Logging;
using Celbridge.Terminal;
using Microsoft.UI.Dispatching;
using Microsoft.Web.WebView2.Core;

namespace Celbridge.Console.Views;

public sealed partial class ConsolePanel : UserControl, IConsolePanel
{
    private ILogger<ConsolePanel> _logger;

    public ConsolePanelViewModel ViewModel { get; }

    private ConPtyTerminal _terminal = new();

    public ConsolePanel()
    {
        this.InitializeComponent();

        _logger = ServiceLocator.AcquireService<ILogger<ConsolePanel>>();
        ViewModel = ServiceLocator.AcquireService<ConsolePanelViewModel>();

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await TerminalWebView.EnsureCoreWebView2Async();

        TerminalWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        TerminalWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("Terminal",
            "Celbridge.Console/Assets/Terminal",
            CoreWebView2HostResourceAccessKind.Allow);

        TerminalWebView.Source = new Uri("http://Terminal/index.html");
    }

    public async Task<Result> ExecuteCommand(string command, bool logCommand)
    {
        await Task.CompletedTask;
        return Result.Ok();
    }

    public async Task<Result> InitializeScripting()
    {
        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

        _terminal.OutputReceived += (_, output) =>
        {
            dispatcher.TryEnqueue(async () =>
            {
                await SendToTerminalAsync(output);
            });
        };

        _terminal.Start("py");

        return await Task.FromResult(Result.Ok());
    }

    private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        string input = args.TryGetWebMessageAsString();

        if (!string.IsNullOrEmpty(input))
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
