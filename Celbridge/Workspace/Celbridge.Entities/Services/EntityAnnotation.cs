using Celbridge.Entities;

namespace Celbridge.Activities.Services;

public class EntityAnnotation : IEntityAnnotation
{
    private readonly List<EntityReportItem> _entityErrors = new();
    private readonly HashSet<int> _recognizedComponents = new();
    private readonly List<ComponentAnnotation> _componentAnnotations = new();
    private readonly ComponentAnnotation _invalidAnnotation;

    public  string ActivityName { get; set; } = string.Empty;

    public EntityAnnotation()
    {
        // Create a default invalid annotation.
        // This is returned for any unrecognized components.
        List<EntityReportItem> errors = new()
        {
            new EntityReportItem(
                EntityReportType.Error, 
                "Invalid component", 
                "This component is not valid in this position.")
        };

        _invalidAnnotation = new ComponentAnnotation(0, errors);
    }

    public void Initialize(int count)
    {
        _componentAnnotations.Clear();
        _recognizedComponents.Clear();

        // Populate the list with empty component annotations.
        for (int i = 0; i < count; i++)
        {
            var annotation = new ComponentAnnotation(0, new());
            _componentAnnotations.Add(annotation);
        }
    }

    public bool TryGetError(out EntityReportItem? entityError)
    {
        entityError = null;

        bool hasError = false;
        if (EntityErrors.Count > 0)
        {
            entityError = EntityErrors[0];
            hasError = true;
        }
        else
        {
            foreach (var componentAnnotation in _componentAnnotations)
            {
                if (componentAnnotation.ReportItems.Count > 0)
                {
                    entityError = componentAnnotation.ReportItems[0];
                    hasError = true;
                    break;
                }
            }
        }

        return hasError;
    }

    public IReadOnlyList<EntityReportItem> EntityErrors => _entityErrors;

    public void AddEntityError(EntityReportItem error)
    {
        _entityErrors.Add(error);

        _entityErrors.Sort((a, b) =>
        {
            return (int)a.ReportType - (int)b.ReportType;
        });
    }

    public void SetIsRecognized(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= _componentAnnotations.Count)
        {
            throw new IndexOutOfRangeException($"Component index is out of range: {componentIndex}");
        }

        _recognizedComponents.Add(componentIndex);
    }

    public void SetIndent(int componentIndex, int indentLevel)
    {
        if (componentIndex < 0 || componentIndex >= _componentAnnotations.Count)
        {
            throw new IndexOutOfRangeException($"Component index is out of range: {componentIndex}");
        }

        var updatedAnnotation = _componentAnnotations[componentIndex] with
        {
            IndentLevel = indentLevel
        };

        _componentAnnotations[componentIndex] = updatedAnnotation;
    }

    public void AddComponentError(int componentIndex, EntityReportItem error)
    {
        if (componentIndex < 0 || componentIndex >= _componentAnnotations.Count)
        {
            throw new IndexOutOfRangeException($"Component index is out of range: {componentIndex}");
        }

        // Adding any error to an annotation automatically marks the component as recognized.
        // This ensures that the error added here will be displayed instead of the general "Invalid component"
        // error for unrecognized components. The error added here is more specific so probably more helpful. 
        SetIsRecognized(componentIndex);

        var annotation = _componentAnnotations[componentIndex];

        var errorList = annotation.ReportItems;
        errorList.Add(error);

        // Sort errors so most severe errors are listed first
        errorList.Sort((a, b) =>
        {
            return (int)a.ReportType - (int)b.ReportType;
        });
    }

    public int ComponentAnnotationCount => _componentAnnotations.Count;

    public ComponentAnnotation GetComponentAnnotation(int componentIndex)
    {
        if (componentIndex < 0 || componentIndex >= _componentAnnotations.Count)
        {
            throw new IndexOutOfRangeException($"Component index is out of range: {componentIndex}");
        }

        if (!_recognizedComponents.Contains(componentIndex))
        {
            // Unrecognized components are annotated as invalid.
            return _invalidAnnotation;
        }

        var annotation = _componentAnnotations[componentIndex];
        return annotation;
    }
}
