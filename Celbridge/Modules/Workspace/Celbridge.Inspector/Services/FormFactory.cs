using Celbridge.Inspector.Models;
using Celbridge.Inspector.Views;
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

        var header = propertyTypeInfo.PropertyName;
        var text = propertyTypeInfo.PropertyType;

        return CreateStringForm(resource, componentIndex, propertyName);
    }

    private Result<IForm> CreateStringForm(ResourceKey resource, int componentIndex, string propertyName)
    {
        var element = new StringForm();
        element.ViewModel.Initialize(resource, componentIndex, propertyName);
        element.TabIndex = componentIndex;

        var form = new Form(element);

        return Result<IForm>.Ok(form);
    }
}
