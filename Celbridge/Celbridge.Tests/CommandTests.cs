using Celbridge.Commands.Services;
using Celbridge.Commands;
using Celbridge.Foundation;
using Celbridge.Logging.Services;
using Celbridge.Logging;
using Celbridge.Messaging.Services;
using Celbridge.Messaging;
using Celbridge.UserInterface.Services;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Celbridge.Tests;

public class TestCommand : CommandBase
{
    public override CommandFlags CommandFlags => CommandFlags.Undoable;

    public bool ExecuteComplete { get; private set; }
    public bool UndoComplete { get; private set; }
    public bool RedoComplete { get; private set; }

    public override async Task<Result> ExecuteAsync()
    {
        await Task.CompletedTask;

        ExecuteComplete = true;

        if (UndoComplete)
        {
            // Assume second time executing is a redo
            RedoComplete = true;
        }

        return Result.Ok();
    }

    public override async Task<Result> UndoAsync()
    {
        await Task.CompletedTask;

        UndoComplete = true;

        return Result.Ok();
    }
}

[TestFixture]
public class CommandTests
{
    private ServiceProvider? _serviceProvider;
    private ICommandService? _commandService;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        Logging.ServiceConfiguration.ConfigureServices(services);
        services.AddSingleton<ILogSerializer, LogSerializer>();
        services.AddSingleton<IMessengerService, MessengerService>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IWorkspaceWrapper, WorkspaceWrapper>();

        _serviceProvider = services.BuildServiceProvider();
        _commandService = _serviceProvider.GetRequiredService<ICommandService>();

        var commandService = _commandService as CommandService;
        Guard.IsNotNull(commandService);
        commandService.StartExecution();
    }

    [TearDown]
    public void TearDown()
    {
        var commandService = _commandService as CommandService;
        Guard.IsNotNull(commandService);
        commandService.StopExecution();

        if (_serviceProvider != null)
        {
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }

    [Test]
    public async Task ICanExecuteACommand()
    {
        Guard.IsNotNull(_commandService);

        var testCommand = new TestCommand();
        _commandService.EnqueueCommand(testCommand);

        // Wait for command to execute
        for (int i = 0; i < 10; i++)
        {
            if (testCommand.ExecuteComplete)
            {
                break;
            }
            await Task.Delay(50);
        }

        testCommand.ExecuteComplete.Should().BeTrue();
    }

    [Test]
    public async Task ICanUndoAndRedoACommand()
    {
        Guard.IsNotNull(_commandService);

        //
        // Execute test command, using an undo stack
        //

        // The undo stack is currently empty
        _commandService.IsUndoStackEmpty().Should().BeTrue();

        var testCommand = new TestCommand();
        _commandService.EnqueueCommand(testCommand);

        // Wait for command to execute
        for (int i = 0; i < 10; i++)
        {
            if (testCommand.ExecuteComplete)
            {
                break;
            }
            await Task.Delay(50);
        }

        testCommand.ExecuteComplete.Should().BeTrue();

        // Undo stack should contain one item
        _commandService.IsUndoStackEmpty().Should().BeFalse();

        //
        // Undo the command
        //

        testCommand.UndoComplete.Should().BeFalse();
        
        var undoResult = _commandService.TryUndo();
        undoResult.IsSuccess.Should().BeTrue();
        undoResult.Value.Should().BeTrue();

        _commandService.IsUndoStackEmpty().Should().BeTrue();

        // Wait for undo to complete
        for (int i = 0; i < 10; i++)
        {
            if (testCommand.UndoComplete)
            {
                break;
            }
            await Task.Delay(50);
        }

        testCommand.UndoComplete.Should().BeTrue();

        //
        // Redo the command
        //

        _commandService.IsRedoStackEmpty().Should().BeFalse();

        var redoResult = _commandService.TryRedo();
        redoResult.IsSuccess.Should().BeTrue();
        redoResult.Value.Should().BeTrue();

        // Wait for redo to complete
        for (int i = 0; i < 10; i++)
        {
            if (testCommand.RedoComplete)
            {
                break;
            }
            await Task.Delay(50);
        }

        _commandService.IsUndoStackEmpty().Should().BeFalse();
        _commandService.IsRedoStackEmpty().Should().BeTrue();
    }
}
