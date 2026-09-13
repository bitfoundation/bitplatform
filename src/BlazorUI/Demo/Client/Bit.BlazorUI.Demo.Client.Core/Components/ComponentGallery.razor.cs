namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// The components index: a searchable, category-filtered, relevance-ranked view over
/// <see cref="ComponentCatalog"/>.
/// <para>
/// It replaces a hand-written list of ~110 links that had to be edited every time a component was
/// added, could not be searched, and said nothing about what any of them were for.
/// </para>
/// <para>
/// It reads in two modes, because browsing and searching are different tasks. With no term the
/// catalog is a reference: every component, grouped and ordered the way the nav groups and orders
/// them, so a reader who knows roughly where to look finds the same thing in the same place every
/// time. With a term it is a result list: one flat run, best match first, because in a search the
/// nav's order is arbitrary and the second-best answer showing up nine categories below the best
/// one is a search that failed.
/// </para>
/// </summary>
public partial class ComponentGallery
{
    /// <summary>
    /// The layouts the results can be read in. The grid is for browsing - a card is a target and the
    /// summary is the point of it; the list is for finding - one line per component, so the
    /// whole catalog fits in a few screens instead of a dozen.
    /// </summary>
    private enum GalleryView { Grid, List }


    private string? _filter;
    private string? _category;
    private GalleryView _view = GalleryView.Grid;

    /// <summary>The catalog grouped by category: what is rendered when there is no search term.</summary>
    private List<ComponentCatalogCategory> _groups = [];

    /// <summary>The ranked matches: what is rendered when there is one.</summary>
    private IReadOnlyList<ComponentCatalogItem> _results = [];

    /// <summary>
    /// How many components each category holds for the current term, keyed by category name. It is
    /// what turns the chip row into a facet: with "picker" typed, the reader can see that the hits
    /// are five Pickers and one Extra before choosing. Deliberately blind to the selected category,
    /// so a chip keeps saying what the term finds in it while a different one is active - which is
    /// the only way the row can be used to move between categories rather than only into one.
    /// </summary>
    private Dictionary<string, int> _counts = [];

    /// <summary>What the term matches in the whole catalog: the count on the "All" chip.</summary>
    private int _totalCount;

    /// <summary>What is actually on screen, once the category has narrowed it: the status line.</summary>
    private int _matchCount;

    // The query values this component itself last wrote. A parameter that still matches one of them
    // is this component's own navigation coming back around, not the reader arriving with a
    // different term, so it must not overwrite what they have since typed (see OnParamsSetAsync).
    private string? _syncedFilter;
    private string? _syncedCategory;

    private bool _isSearching => _filter.HasValue();


    /// <summary>
    /// A term to start filtered by - the header's search box hands one over when Enter did not pick
    /// out a single component (see <see cref="ComponentSearchBox"/>).
    /// </summary>
    [Parameter] public string? InitialFilter { get; set; }

    /// <summary>
    /// A category to start narrowed to. The home page's category grid and the overview's category
    /// list both link here with one, which is what makes those tiles an entry into the catalog
    /// rather than a search that happens to match a category's name.
    /// </summary>
    [Parameter] public string? InitialCategory { get; set; }


    protected override Task OnParamsSetAsync()
    {
        // Only adopt a query value that is not the one this component put there. Anything else is
        // the round trip of its own NavigateTo, and taking it again would undo a keystroke made
        // between the navigation and the re-render.
        if (Differs(InitialFilter, _syncedFilter))
        {
            _syncedFilter = InitialFilter;
            _filter = InitialFilter;
        }

        if (Differs(InitialCategory, _syncedCategory))
        {
            _syncedCategory = InitialCategory;
            _category = Resolve(InitialCategory);
        }

        Rebuild();

        return base.OnParamsSetAsync();
    }


    private void ApplyFilter(string? value)
    {
        _filter = value;
        Rebuild();
        SyncUrl();
    }

    private void SelectCategory(string? category)
    {
        // Clicking the category you are already in clears it, which is what a pressed toggle implies.
        _category = _category == category ? null : category;
        Rebuild();
        SyncUrl();
    }

    private void SelectView(GalleryView view)
    {
        // Deliberately not in the URL: it is how this reader likes to read, not what they are
        // looking at, and a shared link should open on the layout its recipient chose.
        _view = view;
    }

    private void ClearFilters()
    {
        _filter = null;
        _category = null;
        Rebuild();
        SyncUrl();
    }

    /// <summary>
    /// Enter in the search box. The top of a ranked list is what the reader almost always meant, and
    /// making them reach for the mouse to confirm it is the one thing a search box this good should
    /// not ask for. Enter on a term that matches nothing leaves them on the empty state, which says
    /// more than a navigation to nowhere would.
    /// </summary>
    private void GoToTopResult(string? term)
    {
        if (_results.Count == 0) return;

        NavigationManager.NavigateTo(_results[0].Url);
    }

    /// <summary>
    /// Rebuilds what is rendered from the current term and category. Runs over a hundred-odd prebuilt strings,
    /// so it is cheap enough to do on every keystroke without a debounce of its own (the search box
    /// already has one).
    /// </summary>
    private void Rebuild()
    {
        _groups = [];
        _results = [];
        _counts = [];

        if (_isSearching)
        {
            // Ranked rather than filtered: the catalog knows that "tag" means Tag before it means
            // TagsInput, and before it means the six components merely tagged with the word.
            var ranked = ComponentCatalog.Search(_filter);

            foreach (var item in ranked)
            {
                _counts[item.Category] = _counts.GetValueOrDefault(item.Category) + 1;
            }

            _results = _category is null ? ranked : [.. ranked.Where(i => i.Category == _category)];
            _totalCount = ranked.Count;
            _matchCount = _results.Count;

            return;
        }

        foreach (var category in ComponentCatalog.Categories)
        {
            _counts[category.Name] = category.Items.Count;

            if (_category is not null && category.Name != _category) continue;

            _groups.Add(category);
        }

        _totalCount = ComponentCatalog.Items.Count;
        _matchCount = _groups.Sum(g => g.Items.Count);
    }

    /// <summary>
    /// Writes the current term and category into the address bar, so a filtered catalog is a page
    /// that can be linked, bookmarked and reloaded rather than a state that exists only in this tab.
    /// Replaces rather than pushes: a history stack with one entry per keystroke turns the back
    /// button into a way of deleting characters one at a time.
    /// </summary>
    private void SyncUrl()
    {
        _syncedFilter = _filter.HasValue() ? _filter : null;
        _syncedCategory = _category;

        var query = new List<string>();

        if (_syncedFilter is not null) query.Add($"q={Uri.EscapeDataString(_syncedFilter)}");
        if (_syncedCategory is not null) query.Add($"category={Uri.EscapeDataString(_syncedCategory)}");

        var url = query.Count > 0 ? $"/components?{string.Join('&', query)}" : "/components";

        NavigationManager.NavigateTo(url, replace: true);
    }

    /// <summary>
    /// The category a query value names, matched case-insensitively so that a hand-typed
    /// <c>?category=inputs</c> lands on Inputs, and null for one no category answers to - which
    /// leaves the catalog unnarrowed rather than empty.
    /// </summary>
    private static string? Resolve(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return null;

        return ComponentCatalog.Categories
                               .FirstOrDefault(c => string.Equals(c.Name, category.Trim(), StringComparison.OrdinalIgnoreCase))
                               ?.Name;
    }

    private static bool Differs(string? left, string? right)
    {
        return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal) is false;
    }
}
