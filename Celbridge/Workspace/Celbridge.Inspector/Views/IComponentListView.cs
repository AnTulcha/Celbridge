namespace Celbridge.Inspector.Views;

/// <summary>
/// Interface to allow the ComponentListViewModel to interact with the ComponentListView.
/// </summary>
public interface IComponentListView
{
    Result SetComponentSummaryForm(int componentIndex, object form);
}
