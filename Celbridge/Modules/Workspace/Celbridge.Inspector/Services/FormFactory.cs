using Celbridge.Inspector.Models;
using Celbridge.Workspace;

namespace Celbridge.Inspector.Services;

public class FormFactory : IFormFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    public FormFactory(
        IServiceProvider serviceProvider,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _workspaceWrapper = workspaceWrapper;
    }

    public Result<IForm> CreatePropertyForm(ResourceKey resource, int componentIndex, string propertyName)
    {
        var entityService = _workspaceWrapper.WorkspaceService.EntityService;

        // Get the component type info
        var getInfoResult = entityService.GetComponentTypeInfo(resource, componentIndex);
        if (getInfoResult.IsFailure)
        {
            return Result<IForm>.Fail($"Failed to get component type info for resource '{resource}' at component index '{componentIndex}'")
                .WithErrors(getInfoResult);
        }
        var componentTypeInfo = getInfoResult.Value;

        // Get the property type info
        var propertyTypeInfos = componentTypeInfo.Properties.Where(p => p.PropertyName == propertyName).ToList();
        if (propertyTypeInfos.Count != 1)
        {
            return Result<IForm>.Fail($"Failed to find component property '{propertyName}' for resource '{resource}' at component index '{componentIndex}'");
        }
        var propertyTypeInfo = propertyTypeInfos[0];


        // Todo: Create a ViewModel for that property type, that wraps the property value
        // Todo: Create a View for that property type
        // Todo: Return the View

        // Todo: Use humaizer to format the property name

        var header = propertyTypeInfo.PropertyName;
        var text = propertyTypeInfo.PropertyType;

        return CreateStringForm(header, text);
    }

    private Result<IForm> CreateStringForm(string header, string text)
    {
        var textBlock = new TextBox
        {
            Header = header,
            FontSize = 16
        };

        var form = new Form(textBlock);

        return Result<IForm>.Ok(form);
    }
}
