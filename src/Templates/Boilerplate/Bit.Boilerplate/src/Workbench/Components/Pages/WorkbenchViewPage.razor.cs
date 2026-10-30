namespace Boilerplate.Workbench.Components.Pages;

public partial class WorkbenchViewPage
{
    private WorkbenchPageInfo? pageInfo;

    [Parameter] public string? PageKey { get; set; }

    protected override void OnParametersSet()
    {
        pageInfo = WorkbenchPageCatalog.Find(PageKey);
    }
}
