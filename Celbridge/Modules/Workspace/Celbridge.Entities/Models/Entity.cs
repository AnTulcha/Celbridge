using System.Text.Json;
using System.Text.Json.Nodes;
using Celbridge.Messaging;
using CommunityToolkit.Diagnostics;

using Path = System.IO.Path;

namespace Celbridge.Entities.Models;

public class Entity
{
    private readonly IMessengerService _messengerService;

    private EntityData? _entityData;

    private static JsonSerializerOptions _serializationOptions = new()
    {
        WriteIndented = true
    };

    public ResourceKey Resource { get; private set; }
    public string EntityDataPath { get; private set; } = string.Empty;

    public Entity(IMessengerService messengerService)
    {
        _messengerService = messengerService;
    }

    public void SetEntityData(EntityData entityData)
    {
        _entityData = entityData;
    }

    public void SetResourceKey(ResourceKey resource, string entityDataPath)
    {
        Resource = resource;
        EntityDataPath = entityDataPath;
    }

    public async Task<Result> SaveAsync()
    {
        try
        {
            Guard.IsNotNull(_entityData);

            var jsonContent = JsonSerializer.Serialize(_entityData.JsonObject, _serializationOptions);

            var folder = Path.GetDirectoryName(EntityDataPath);
            Guard.IsNotNull(folder);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (var writer = new StreamWriter(EntityDataPath))
            {
                await writer.WriteAsync(jsonContent);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to save entity data for '{Resource}'")
                .WithException(ex);
        }
    }

    public T? GetProperty<T>(string propertyName, T? defaultValue)
        where T : notnull
    {
        var getResult = GetPropertyChecked<T>(propertyName);
        if (getResult.IsFailure)
        {
            return defaultValue;
        }

        return getResult.Value;
    }

    public bool SetProperty<T>(string propertyName, T newValue) 
        where T : notnull
    {
        var setResult = SetPropertyChecked(propertyName, newValue);
        if (setResult.IsFailure)
        {
            var failure = Result<T>.Fail($"Failed to set property '{propertyName}'")
                .WithErrors(setResult);

            throw new InvalidOperationException(failure.Error);
        }

        return setResult.Value;
    }

    private Result<T> GetPropertyChecked<T>(string propertyName) 
        where T : notnull
    {
        try
        {
            var jsonObject = _entityData!.JsonObject;

            if (jsonObject != null && jsonObject.ContainsKey(propertyName))
            {
                var valueNode = jsonObject[propertyName];
                if (valueNode != null)
                {
                    T? value = valueNode.Deserialize<T>();
                    if (value is not null)
                    {
                        return Result<T>.Ok(value);
                    }
                }
            }

            // Property value not found
            return Result<T>.Fail();
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"An exception occurred when getting entity property '{propertyName}' for resource '{Resource}'")
                .WithException(ex);
        }
    }

    private Result<bool> SetPropertyChecked<T>(string propertyName, T newValue) where T : notnull
    {
        try
        {
            var jsonObject = _entityData!.JsonObject as JsonObject;

            if (jsonObject == null)
            {
                return Result<bool>.Fail("Invalid JSON structure.");
            }

            var oldNode = jsonObject[propertyName];
            var newNode = JsonNode.Parse(JsonSerializer.Serialize(newValue));

            if (oldNode is null && newNode is null)
            {
                // No change
                return Result<bool>.Ok(false);
            }

            if (newNode is not null && oldNode is not null &&
                newNode.GetValueKind() == oldNode.GetValueKind() && 
                newNode.ToJsonString() == oldNode.ToJsonString())
            {
                // No change
                return Result<bool>.Ok(false);
            }

            // Update the JSON data
            jsonObject[propertyName] = newNode;

            var message = new EntityPropertyChangedMessage(Resource, propertyName, EntityPropertyChangeType.Update);
            _messengerService.Send(message);

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to set entity property '{propertyName}' for resource '{Resource}'")
                .WithException(ex);
        }
    }
}
