using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Patch;
using CommunityToolkit.Diagnostics;
using Json.Pointer;

namespace Celbridge.Entities.Models;

public class EntityData
{
    public JsonObject JsonObject { get; private set; }
    public EntitySchema EntitySchema { get; }

    private EntityData(JsonObject jsonObject, EntitySchema entitySchema)
    {
        JsonObject = jsonObject;
        EntitySchema = entitySchema;
    }

    public static EntityData Create(JsonObject jsonObject, EntitySchema entitySchema)
    {
        return new EntityData(jsonObject, entitySchema);
    }

    public EntityData DeepClone()
    {
        var jsonClone = JsonObject.DeepClone() as JsonObject;
        Guard.IsNotNull(jsonClone);

        return new EntityData(jsonClone, EntitySchema);
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

    private Result<T> GetPropertyChecked<T>(string propertyPath)
        where T : notnull
    {
        try
        {
            var jsonPointer = JsonPointer.Parse(propertyPath);
            if (jsonPointer is null)
            {
                return Result<T>.Fail($"Invalid JSON pointer '{propertyPath}'");
            }

            if (!jsonPointer.TryEvaluate(JsonObject, out var valueNode))
            {
                return Result<T>.Fail($"Property was not found at: '{propertyPath}'");
            }

            if (valueNode is null)
            {
                // The property was fgound but it is a JSON null value.
                // We treat this as an error for Entity Data.
                return Result<T>.Fail($"Property is a JSON null value: '{propertyPath}'");
            }

            var value = valueNode.Deserialize<T>();
            if (value is null)
            {
                return Result<T>.Fail($"Failed to deserialize property at '{propertyPath}' to type '{nameof(T)}'");
            }
            
            return Result<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"An exception occurred when getting entity property '{propertyPath}'")
                .WithException(ex);
        }
    }

    private Result<bool> SetPropertyChecked<T>(string propertyName, T newValue) where T : notnull
    {
        try
        {
            var oldNode = JsonObject[propertyName];
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
            JsonObject[propertyName] = newNode;

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to set entity property '{propertyName}'")
                .WithException(ex);
        }
    }

    public Result<EntityPatchSummary> ApplyPatch(string patchJson)
    {
        try
        {
            var patch = JsonSerializer.Deserialize<JsonPatch>(patchJson);
            if (patch is null)
            {
                return Result<EntityPatchSummary>.Fail("Failed to deserialize JSON patch");
            }

            var patchResult = patch.Apply(JsonObject);
            if (!patchResult.IsSuccess)
            {
                return Result<EntityPatchSummary>.Fail($"Failed to apply JSON patch to entity data: {patchResult.Error}");
            }

            var newJsonObject = patchResult.Result as JsonObject;
            Guard.IsNotNull(newJsonObject);

            // Check if the JSON object has actually changed as a result of applying the patch
            if (JsonNode.DeepEquals(JsonObject, newJsonObject))
            {
                // The patch was valid, but did not result in any changes.
                // This is indicated by returning an empty path list and reverse patch.
                var emptyAppliedPatch = new EntityPatchSummary(new List<string>(), patchJson, string.Empty);
                return Result<EntityPatchSummary>.Ok(emptyAppliedPatch);
            }

            // Check if the patched JSON is still valid for the entity schema
            var validationResult = EntitySchema.ValidateJsonObject(newJsonObject);
            if (validationResult.IsFailure)
            {
                return Result<EntityPatchSummary>.Fail($"Failed to apply JSON patch to entity data")
                    .WithErrors(validationResult);
            }

            // Generate the reverse patch so we can undo the change if needed.
            var reversePatch = newJsonObject.CreatePatch(JsonObject);
            var reversePatchJson = JsonSerializer.Serialize(reversePatch);

            // Note each path that was actually changed by the patch.
            // It is possible that not all operations in the original resulted in a change.
            List<string> paths = new();
            reversePatch.Operations.ForEach(op =>
            {
                var jsonPointer = op.Path;
                var path = jsonPointer.ToString();
                paths.Add(path);
            });
            var patchSummary = new EntityPatchSummary(paths, patchJson, reversePatchJson);

            // Use the patched JSON object
            JsonObject = newJsonObject;

            return Result<EntityPatchSummary>.Ok(patchSummary); 
        }
        catch (Exception ex)
        {
            return Result<EntityPatchSummary>.Fail($"An exception occurred when applying JSON patch to entity data.")
                .WithException(ex);
        }
    }
}
