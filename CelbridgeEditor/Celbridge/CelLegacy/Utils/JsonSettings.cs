﻿using Newtonsoft.Json.Serialization;

namespace CelLegacy.Utils;

public static class JsonSettings
{
    public static JsonSerializerSettings Create()
    {
        void OnError(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            e.ErrorContext.Handled = true;
        }

        var settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            Error = OnError,
            ContractResolver = new DefaultContractResolver(), // This should only cache types for this instance of the settings
        };

        return settings;
    }
}