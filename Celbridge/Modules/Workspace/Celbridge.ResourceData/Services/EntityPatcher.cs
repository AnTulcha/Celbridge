using Newtonsoft.Json.Linq;

namespace Celbridge.ResourceData;

public class EntityPatcher
{
    private readonly List<Func<JObject, Result>> _operations = new();
    private readonly List<Func<JObject, Result>> _undoOperations = new();

    public void Set(string path, JToken value)
    {
        _operations.Add(json =>
        {
            var token = json.SelectToken(path);
            var parent = json.SelectToken(path.ParentPath()) as JObject;

            if (parent == null)
            {
                return Result.Fail($"Set operation failed: Parent path '{path.ParentPath()}' not found.");
            }

            if (token != null)
            {
                var originalValue = token.DeepClone();
                token.Replace(value);
                _undoOperations.Insert(0, json => 
                { 
                    json.SelectToken(path)?.Replace(originalValue); 
                    return Result.Ok(); 
                });
            }
            else
            {
                parent[path.LastSegment()] = value;
                _undoOperations.Insert(0, json => 
                {
                    var parentToken = parent[path.LastSegment()];

                    if (parentToken is not null)
                    {
                        // Ensure we remove the property from its parent
                        if (parentToken.Parent is JProperty property)
                        {
                            property.Remove();
                        }
                        else
                        {
                            parentToken.Remove();
                        }
                    }

                    return Result.Ok(); 
                });
            }

            return Result.Ok();
        });
    }

    public void Remove(string path)
    {
        _operations.Add(json =>
        {
            var token = json.SelectToken(path);
            if (token == null)
            {
                return Result.Fail($"Remove operation failed: Path '{path}' not found.");
            }

            var originalValue = token.DeepClone();

            // Ensure we remove the property from its parent
            if (token.Parent is JProperty property)
            {
                property.Remove();
            }
            else
            {
                token.Remove();
            }

            _undoOperations.Insert(0, json => 
            {
                var parentToken = json.SelectToken(path.ParentPath());
                if (parentToken is not null)
                {
                    parentToken[path.LastSegment()] = originalValue;
                }                

                return Result.Ok(); 
            });

            return Result.Ok();
        });
    }

    public void Move(string fromPath, string toPath)
    {
        _operations.Add(json =>
        {
            var fromToken = json.SelectToken(fromPath);
            if (fromToken == null)
            {
                return Result.Fail($"Move operation failed: Path '{fromPath}' not found.");
            }

            var toParent = json.SelectToken(toPath.ParentPath()) as JObject;
            if (toParent == null)
            {
                return Result.Fail($"Move operation failed: Target path '{toPath.ParentPath()}' not found.");
            }

            var movedValue = fromToken.DeepClone();

            // Ensure we remove the property from its parent
            if (fromToken.Parent is JProperty property)
            {
                property.Remove();
            }
            else
            {
                fromToken.Remove();
            }

            toParent[toPath.LastSegment()] = movedValue;

            _undoOperations.Insert(0, json => 
            { 
                var toToken = toParent[toPath.LastSegment()];

                if (toToken == null)
                {
                    return Result.Fail($"Undo move operation failed: Path '{toPath}' not found.");
                }

                // Ensure we remove the property from its parent
                if (toToken.Parent is JProperty property)
                {
                    property.Remove();
                }
                else
                {
                    toToken.Remove();
                }

                var parentToken = json.SelectToken(fromPath.ParentPath());
                if (parentToken is not null)
                {
                    parentToken[fromPath.LastSegment()] = movedValue;
                }
                return Result.Ok(); 
            });

            return Result.Ok();
        });
    }

    public void Copy(string fromPath, string toPath)
    {
        _operations.Add(json =>
        {
            var fromToken = json.SelectToken(fromPath);
            if (fromToken == null)
                return Result.Fail($"Copy operation failed: Path '{fromPath}' not found.");

            var toParent = json.SelectToken(toPath.ParentPath()) as JObject;
            if (toParent == null)
                return Result.Fail($"Copy operation failed: Target path '{toPath.ParentPath()}' not found.");

            toParent[toPath.LastSegment()] = fromToken.DeepClone();
            _undoOperations.Insert(0, json => 
            {
                var parent = toParent[toPath.LastSegment()];
                if (parent is not null)
                {
                    // Ensure we remove the property from its parent
                    if (parent.Parent is JProperty property)
                    {
                        property.Remove();
                    }
                    else
                    {
                        parent.Remove();
                    }
                }
                return Result.Ok(); 
            });

            return Result.Ok();
        });
    }

    public void Test(string path, JToken expectedValue)
    {
        _operations.Add(json =>
        {
            var token = json.SelectToken(path);
            if (token == null || !JToken.DeepEquals(token, expectedValue))
            {
                return Result.Fail($"Test operation failed: Value at '{path}' does not match expected value.");
            }

            return Result.Ok();
        });
    }

    public Result Apply(JObject json)
    {
        foreach (var operation in _operations)
        {
            var result = operation(json);
            if (result.IsFailure)
            {
                return result;
            }
        }
        return Result.Ok();
    }

    public Result Undo(JObject json)
    {
        foreach (var undoOperation in _undoOperations)
        {
            var result = undoOperation(json);
            if (result.IsFailure)
            {
                return result;
            }
        }
        return Result.Ok();
    }
}

public static class JsonPathExtensions
{
    public static string ParentPath(this string path) => path.Contains('.') ? path.Substring(0, path.LastIndexOf('.')) : string.Empty;
    public static string LastSegment(this string path) => path.Contains('.') ? path.Substring(path.LastIndexOf('.') + 1) : path;
}
