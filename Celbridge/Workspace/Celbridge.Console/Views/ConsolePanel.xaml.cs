using Celbridge.Console.ViewModels;
using Celbridge.Logging;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using Celbridge.Terminal;
using Microsoft.UI.Dispatching;

namespace Celbridge.Console.Views;

public sealed partial class ConsolePanel : UserControl, IConsolePanel
{
    private int _rpcId = 0;

    private ILogger<ConsolePanel> _logger;

    public ConsolePanelViewModel ViewModel { get; }

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
        await Task.CompletedTask;

        DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();

        var terminal = new ConPtyTerminal();

        terminal.OutputReceived += (_, output) =>
        {
            dispatcher.TryEnqueue(async () =>
            {
                await SendToTerminalAsync(output);
            });
        };

        terminal.Start("py");

        return Result.Ok();
    }

    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        using var outerDoc = JsonDocument.Parse(args.WebMessageAsJson);
        string innerJson = outerDoc.RootElement.GetString() ?? string.Empty;

        var json = innerJson;
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("method", out var methodProperty))
        {
            return;
        }

        var method = methodProperty.GetString();

        if (method == "terminalInput")
        {
            var input = root.GetProperty("params").GetProperty("data").GetString();
            if (input == "\r")
            {
                input = Environment.NewLine;
            }

            _logger.LogInformation($"Terminal input: {input}");

            await SendToTerminalAsync(input);
        }
    }

    private async Task SendToTerminalAsync(string text)
    {
        var message = new
        {
            jsonrpc = "2.0",
            method = "writeToTerminal",
            @params = new { data = text },
            id = _rpcId++
        };
        string json = JsonSerializer.Serialize(message);
        TerminalWebView.CoreWebView2.PostWebMessageAsJson(json);

        await Task.CompletedTask;
    }
}
