﻿namespace Celbridge.BaseLibrary.Settings;

/// <summary>
/// Read and write user settings that persist between application sessions.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Stores a value with the provided key.
    /// </summary>
    Result SetValue<T>(string settingKey, T value) where T : notnull;

    /// <summary>
    /// Returns a previously stored value with the provided key.
    /// Fails if the requested value was not found or could not be deserialized to the
    /// requested type.
    /// </summary>
    Result<T> GetValue<T>(string settingKey) where T : notnull;
}