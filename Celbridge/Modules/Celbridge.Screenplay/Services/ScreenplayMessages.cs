namespace Celbridge.Screenplay.Services;

/// <summary>
/// Message sent when an error is found while attempting to save a screenplay.
/// </summary>
/// <param name="SceneResource"></param>
public record SaveScreenplayErrorMessage(ResourceKey SceneResource);
