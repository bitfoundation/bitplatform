namespace Boilerplate.Workbench.Components.Shell;

public partial class PageNavigator
{
    private string? searchText;

    [Parameter] public string? CurrentKey { get; set; }

    [Parameter] public string? Query { get; set; }

    [Parameter] public EventCallback OnNavigate { get; set; }

    private IEnumerable<WorkbenchPageInfo> FilteredPages => string.IsNullOrWhiteSpace(searchText)
        ? WorkbenchPageCatalog.Pages
        : WorkbenchPageCatalog.Pages.Where(p => Localizer[p.TitleKey].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                                             || Localizer[p.DescriptionKey].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                                             || p.Showcases.Any(s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
}
