using Celbridge.Console.Services;
using Celbridge.Console.ViewModels;
using Celbridge.Console.Views;
using Celbridge.Modules;

namespace Celbridge.Console;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IModuleServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<IConsoleService, ConsoleService>();
        config.AddTransient<ICommandHistory, CommandHistory>();

        //
        // Register views
        //

        config.AddTransient<IConsolePanel, ConsolePanel>();

        //
        // Register view models
        //

        config.AddTransient<ConsolePanelViewModel>();

        //
        // Register commands
        // 

        config.AddTransient<IClearCommand, ClearCommand>();
        config.AddTransient<IClearHistoryCommand, ClearHistoryCommand>();
        config.AddTransient<IUndoCommand, UndoCommand>();
        config.AddTransient<IRedoCommand, RedoCommand>();
        config.AddTransient<IHelpCommand, HelpCommand>();
        config.AddTransient<IPrintCommand, PrintCommand>();
        config.AddTransient<IRunCommand, RunCommand>();
    }
}
