﻿using Celbridge.BaseLibrary.Commands.Project;
using Celbridge.BaseLibrary.Project;
using Celbridge.BaseLibrary.Navigation;
using Celbridge.BaseLibrary.Workspace;
using Celbridge.Commands.Utils;

namespace Celbridge.Commands.Project;

public class CreateProjectCommand : CommandBase, ICreateProjectCommand
{
    private readonly IWorkspaceWrapper _workspaceWrapper;
    private readonly IProjectDataService _projectDataService;
    private readonly INavigationService _navigationService;

    public CreateProjectCommand(
        IWorkspaceWrapper workspaceWrapper,
        IProjectDataService projectDataService,
        INavigationService navigationService)
    {
        _workspaceWrapper = workspaceWrapper;
        _projectDataService = projectDataService;
        _navigationService = navigationService;
    }

    public NewProjectConfig? Config { get; set; }

    public override async Task<Result> ExecuteAsync()
    {
        if (Config is null)
        {
            return Result.Fail("Failed to create new project because config is null.");
        }

        var projectFilePath = Config.ProjectFilePath;

        if (File.Exists(projectFilePath))
        {
            return Result.Fail($"Failed to create project because it already exists: {projectFilePath}");
        }

        // Close any open project.
        // This will fail if there's no project currently open, but we can just ignore that.
        await ProjectUtils.UnloadProjectAsync(_workspaceWrapper, _navigationService, _projectDataService);

        // Create the new project
        var createResult = await ProjectUtils.CreateProjectAsync(_projectDataService, Config);
        if (createResult.IsFailure)
        {
            return Result.Fail($"Failed to create new project. {createResult.Error}");
        }

        // Load the new project
        var loadResult = await ProjectUtils.LoadProjectAsync(_workspaceWrapper, _navigationService, _projectDataService, projectFilePath);
        if (loadResult.IsFailure)
        {
            return Result.Fail($"Failed to load new project. {loadResult.Error}");
        }

        return Result.Ok();
    }
}