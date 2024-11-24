using CommunityToolkit.Diagnostics;
using Json.Patch;
using Json.Pointer;
using System.Text.Json.Nodes;
using System.Text.Json;
using Celbridge.Entities.Services;

namespace Celbridge.Entities.Models;

public class ComponentData
{
    public JsonObject JsonObject { get; private set; }
    public ComponentSchema ComponentSchema { get; }

    private ComponentData(JsonObject jsonObject, ComponentSchema componentSchema)
    {
        JsonObject = jsonObject;
        ComponentSchema = componentSchema;
    }

    public static ComponentData Create(JsonObject jsonObject, ComponentSchema componentSchema)
    {
        return new ComponentData(jsonObject, componentSchema);
    }

    public ComponentData DeepClone()
    {
        var jsonClone = JsonObject.DeepClone() as JsonObject;
        Guard.IsNotNull(jsonClone);

        return new ComponentData(jsonClone, ComponentSchema);
    }

    public Result<T> GetProperty<T>(string propertyPath)
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
                // The property was found but it is a JSON null value.
                // We treat this as an error for Entity Data.
                return Result<T>.Fail($"Property is a JSON null value: '{propertyPath}'");
            }

            var value = valueNode.Deserialize<T>(EntityService.SerializerOptions);
            if (value is null)
            {
                return Result<T>.Fail($"Failed to deserialize component property at '{propertyPath}' to type '{nameof(T)}'");
            }
            
            return Result<T>.Ok(value);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail($"An exception occurred when getting component property '{propertyPath}'")
                .WithException(ex);
        }
    }

    public Result<string> GetPropertyAsJSON(string propertyPath)
    {
        try
        {
            var jsonPointer = JsonPointer.Parse(propertyPath);
            if (jsonPointer is null)
            {
                return Result<string>.Fail($"Invalid JSON pointer '{propertyPath}'");
            }

            if (!jsonPointer.TryEvaluate(JsonObject, out var valueNode))
            {
                return Result<string>.Fail($"Component property was not found at: '{propertyPath}'");
            }

            if (valueNode is null)
            {
                // The property was found but it is a JSON null value.
                // We treat this as an error for Component Data.
                return Result<string>.Fail($"Component property is a JSON null value: '{propertyPath}'");
            }

            var valueJSON = valueNode.ToJsonString();

            return Result<string>.Ok(valueJSON);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"An exception occurred when getting component property '{propertyPath}'")
                .WithException(ex);
        }
    }

    public Result<PatchSummary> ApplyPatch(string patchJson)
    {
        try
        {
            var patch = JsonSerializer.Deserialize<JsonPatch>(patchJson);
            if (patch is null)
            {
                return Result<PatchSummary>.Fail("Failed to deserialize JSON patch");
            }

            var patchResult = patch.Apply(JsonObject);
            if (!patchResult.IsSuccess)
            {
                return Result<PatchSummary>.Fail($"Failed to apply JSON patch to entity data: {patchResult.Error}");
            }

            var newJsonObject = patchResult.Result as JsonObject;
            Guard.IsNotNull(newJsonObject);

            // Check if the JSON object has actually changed as a result of applying the patch
            if (JsonNode.DeepEquals(JsonObject, newJsonObject))
            {
                // The patch was valid, but did not result in any changes.
                // This is indicated by returning an empty path list and reverse patch.
                var emptyAppliedPatch = new PatchSummary(new List<string>(), patchJson, string.Empty);
                return Result<PatchSummary>.Ok(emptyAppliedPatch);
            }

            // Check if the patched JSON is still valid for the entity schema
            var validationResult = ComponentSchema.ValidateJsonObject(newJsonObject);
            if (validationResult.IsFailure)
            {
                return Result<PatchSummary>.Fail($"Failed to apply JSON patch to entity data")
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
            var patchSummary = new PatchSummary(paths, patchJson, reversePatchJson);

            // Use the patched JSON object
            JsonObject = newJsonObject;

            return Result<PatchSummary>.Ok(patchSummary); 
        }
        catch (Exception ex)
        {
            return Result<PatchSummary>.Fail($"An exception occurred when applying JSON patch to component data.")
                .WithException(ex);
        }
    }
}
