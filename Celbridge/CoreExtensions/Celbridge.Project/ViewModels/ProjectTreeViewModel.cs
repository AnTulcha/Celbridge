﻿using Celbridge.BaseLibrary.Project;
using Celbridge.BaseLibrary.UserInterface;
using Celbridge.Project.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Celbridge.Project.ViewModels;

public partial class ProjectTreeViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;
    private readonly IProjectService _projectService;

    private ObservableCollection<Resource> _children = new();
    public ObservableCollection<Resource> Children
    {
        get
        {
            return _children;
        }
        set
        {
            SetProperty(ref _children, value);
        }
    }

    public ProjectTreeViewModel(
        ILoggingService loggingService,
        IUserInterfaceService userInterface)
    {
        _loggingService = loggingService;
        _projectService = userInterface.WorkspaceService.ProjectService;

        ScanProjectFolder();
    }

    private void ScanProjectFolder()
    {
        var projectFolder = _projectService.LoadedProjectData.ProjectFolder;

        _loggingService.Info(projectFolder);
    }
}