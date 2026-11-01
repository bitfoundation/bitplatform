namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Workbench's pages by category, with a search over their titles, descriptions and the components they show off.
/// Its links carry the current views' query, so the next page opens on the same devices and settings.
/// </summary>
public partial class PageNavigator
{
    private string? searchText;

    [Parameter] public string? CurrentKey { get; set; }

    /// <summary>The current views' query, which the link to the current page carries as it is.</summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>Raised when a link is clicked, so the phone layout can close its tools panel.</summary>
    [Parameter] public EventCallback OnNavigate { get; set; }

    /// <summary>
    /// The query a page link carries: the current one for the current page, and for another page the same views
    /// without the current page's design option and switches.
    /// </summary>
    private string? GetQuery(WorkbenchPageInfo page) => page.Key == CurrentKey ? Query : WorkbenchStore.OtherPageQueryString;

    private IEnumerable<WorkbenchPageInfo> FilteredPages => string.IsNullOrWhiteSpace(searchText)
        ? WorkbenchPageCatalog.Pages
        : WorkbenchPageCatalog.Pages.Where(p => Localizer[p.TitleKey].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                                             || Localizer[p.DescriptionKey].Value.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                                             || p.Showcases.Any(s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
}
