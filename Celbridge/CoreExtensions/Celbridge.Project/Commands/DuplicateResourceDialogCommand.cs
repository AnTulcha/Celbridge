﻿using Celbridge.BaseLibrary.Commands;
using Celbridge.BaseLibrary.Dialog;
using Celbridge.BaseLibrary.Project;
using Celbridge.BaseLibrary.Resources;
using Celbridge.BaseLibrary.Validators;
using Celbridge.BaseLibrary.Workspace;
using Microsoft.Extensions.Localization;

namespace Celbridge.Project.Commands;

public class DuplicateResourceDialogCommand : CommandBase, IDuplicateResourceDialogCommand
{
    public override string UndoStackName => UndoStackNames.None;

    public ResourceKey Resource { get; set; }

    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;
    private readonly IStringLocalizer _stringLocalizer;
    private readonly ICommandService _commandService;
    private readonly IWorkspaceWrapper _workspaceWrapper;
    private readonly IDialogService _dialogService;

    public DuplicateResourceDialogCommand(
        IServiceProvider serviceProvider,
        IMessengerService messengerService,
        IStringLocalizer stringLocalizer,
        ICommandService commandService,
        IWorkspaceWrapper workspaceWrapper,
        IDialogService dialogService)
    {
        _serviceProvider = serviceProvider;
        _messengerService = messengerService;
        _stringLocalizer = stringLocalizer;
        _commandService = commandService;
        _workspaceWrapper = workspaceWrapper;
        _dialogService = dialogService;
    }

    public override async Task<Result> ExecuteAsync()
    {
        return await ShowDuplicateResourceDialogAsync();
    }

    private async Task<Result> ShowDuplicateResourceDialogAsync()
    {
        if (!_workspaceWrapper.IsWorkspacePageLoaded)
        {
            return Result.Fail($"Failed to show duplicate resource dialog because workspace is not loaded");
        }

        var resourceRegistry = _workspaceWrapper.WorkspaceService.ProjectService.ResourceRegistry;

        var getResult = resourceRegistry.GetResource(Resource);
        if (getResult.IsFailure)
        {
            return Result.Fail(getResult.Error);
        }
        var resource = getResult.Value;

        var resourceName = resource.Name;

        Range selectedRange;
        switch (resource)
        {
            case IFileResource:
                selectedRange = new Range(0, Path.GetFileNameWithoutExtension(resourceName).Length); // Don't select extension
                break;
            case IFolderResource:
                selectedRange = new Range(0, resourceName.Length);
                break;
            default:
                throw new ArgumentException();
        }

        var duplicateResourceString = _stringLocalizer.GetString("ResourceTree_DuplicateResource", resourceName);

        var defaultText = resourceName;

        var validator = _serviceProvider.GetRequiredService<IResourceNameValidator>();
        validator.ParentFolder = resource.ParentFolder;
        validator.ValidNames.Add(resourceName); // The original name is always valid when renaming

        var enterNameString = _stringLocalizer.GetString("ResourceTree_DuplicateResourceEnterName");

        var showResult = await _dialogService.ShowInputTextDialogAsync(
            duplicateResourceString,
            enterNameString,
            defaultText,
            selectedRange,
            validator);

        if (showResult.IsSuccess)
        {
            var inputText = showResult.Value;

            var sourceParentResource = Resource.GetParent();
            var destResource = sourceParentResource.Combine(inputText);

            if (Resource == destResource)
            {
                // Choosing the original name is treated as a cancel.
                return Result.Ok();
            }

            bool isFolderResource = resource is IFolderResource;

            // Maintain the expanded state of folders after rename
            bool isExpandedFolder = isFolderResource &&
                resourceRegistry.IsFolderExpanded(Resource);

            // Execute a command to move the folder resource to perform the rename
            _commandService.Execute<ICopyResourceCommand>(command =>
            {
                command.SourceResource = Resource;
                command.DestResource = destResource;

                if (isExpandedFolder)
                {
                    command.ExpandCopiedFolder = true;
                }
            });

            var message = new RequestResourceTreeUpdateMessage();
            _messengerService.Send(message);
        }

        return Result.Ok();
    }

    //
    // Static methods for scripting support.
    //

    public static void DuplicateResourceDialog(ResourceKey resource)
    {
        var commandService = ServiceLocator.ServiceProvider.GetRequiredService<ICommandService>();
        commandService.Execute<IDuplicateResourceDialogCommand>(command =>
        {
            command.Resource = resource;
        });
    }
}