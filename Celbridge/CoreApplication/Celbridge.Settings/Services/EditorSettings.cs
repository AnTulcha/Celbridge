﻿using Celbridge.BaseLibrary.Settings;

namespace Celbridge.Settings.Services;

public class EditorSettings : ObservableSettings, IEditorSettings
{
    public EditorSettings(ISettingsGroup settingsGroup)
        : base(settingsGroup, nameof(EditorSettings))
    {}

    public bool IsLeftPanelVisible
    {
        get => GetValue<bool>(nameof(IsLeftPanelVisible), true);
        set => SetValue(nameof(IsLeftPanelVisible), value);
    }

    public float LeftPanelWidth
    {
        get => GetValue<float>(nameof(LeftPanelWidth), 250);
        set => SetValue(nameof(LeftPanelWidth), value);
    }

    public bool IsRightPanelVisible
    {
        get => GetValue<bool>(nameof(IsRightPanelVisible), true);
        set => SetValue(nameof(IsRightPanelVisible), value);
    }

    public float RightPanelWidth
    {
        get => GetValue<float>(nameof(RightPanelWidth), 250);
        set => SetValue(nameof(RightPanelWidth), value);
    }

    public bool IsBottomPanelVisible
    {
        get => GetValue<bool>(nameof(IsBottomPanelVisible), true);
        set => SetValue(nameof(IsBottomPanelVisible), value);
    }

    public float BottomPanelHeight
    {
        get => GetValue<float>(nameof(BottomPanelHeight), 200);
        set => SetValue(nameof(BottomPanelHeight), value);
    }

    public float DetailPanelHeight
    {
        get => GetValue<float>(nameof(DetailPanelHeight), 200);
        set => SetValue(nameof(DetailPanelHeight), value);
    }

    public string PreviousNewProjectFolder
    {
        get => GetValue<string>(nameof(PreviousNewProjectFolder), string.Empty);
        set => SetValue(nameof(PreviousNewProjectFolder), value);
    }

    public string PreviousLoadedProject
    {
        get => GetValue<string>(nameof(PreviousLoadedProject), string.Empty);
        set => SetValue(nameof(PreviousLoadedProject), value);
    }

    public List<string> PreviousOpenDocuments
    {
        get => GetValue<List<string>>(nameof(PreviousOpenDocuments), new());
        set => SetValue(nameof(PreviousOpenDocuments), value);
    }

    public string OpenAIKey
    {
        get => GetValue<string>(nameof(OpenAIKey), string.Empty);
        set => SetValue(nameof(OpenAIKey), value);
    }

    public string SheetsAPIKey
    {
        get => GetValue<string>(nameof(SheetsAPIKey), string.Empty);
        set => SetValue(nameof(SheetsAPIKey), value);
    }
}