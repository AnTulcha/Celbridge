﻿using CommunityToolkit.Mvvm.Messaging;

namespace CelLegacy.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IMessenger _messengerService;
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private bool _isProjectActive;

    public event Action<bool>? WindowActivated;

    public void NotifyWindowActivated(bool active)
    {
        WindowActivated?.Invoke(active);
    }

    public ShellViewModel(IMessenger messengerService, IProjectService projectService)
    {
        _messengerService = messengerService;
        _projectService = projectService;

        _messengerService.Register<ActiveProjectChangedMessage>(this, OnActiveProjectChanged);
    }

    private void OnActiveProjectChanged(object r, ActiveProjectChangedMessage m)
    {
        IsProjectActive = _projectService.ActiveProject != null;
    }
}