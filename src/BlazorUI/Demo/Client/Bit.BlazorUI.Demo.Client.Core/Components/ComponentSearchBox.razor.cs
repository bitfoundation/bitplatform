namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// The header's component finder: type a few characters, get the matching components with the
/// category they belong to, press Enter or click to go there.
/// <para>
/// It searches <see cref="ComponentCatalog"/>, so it matches on more than the visible name - the
/// aliases another library would use ("Select", "Switch", "Chip"), the category, and the one-line
/// summary. Someone who knows what they want but not what bit calls it still lands on the page.
/// </para>
/// <para>
/// The catalog also ranks what it matched, so the component whose own name the term names leads the
/// list, then the ones it is also known by, then the ones it merely describes. With only eight rows
/// in the callout, a Tag that arrives below the components tagged with it is a Tag nobody finds.
/// </para>
/// </summary>
public partial class ComponentSearchBox
{
    /// <summary>
    /// The suggestions are the component names, so everything a picked row needs - the route to
    /// navigate to, the category shown under it - is looked up by name from here.
    /// </summary>
    private static readonly Dictionary<string, ComponentCatalogItem> _byName =
        ComponentCatalog.Items.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ties the keyboard shortcut to this instance's input. Unique per instance because the header,
    /// the hero and the gallery can each have one on screen; the first one registered claims the
    /// shortcut, and keeps it for as long as its element is in the DOM.
    /// </summary>
    private readonly string _rootId = $"cmp-search-{Guid.NewGuid():N}";

    private bool _shortcutRegistered;


    [Parameter] public string? Class { get; set; }

    [Parameter] public string Placeholder { get; set; } = "Search components";

    /// <summary>
    /// The size of the box. The header's copy takes the default that fits a toolbar row; the home
    /// page's hero asks for the large one, where the box is a section of the page rather than a
    /// control in a bar.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Whether Ctrl/Cmd+K (and a bare "/") should put the caret in this box. The header's copy asks
    /// for it on every page that has one, and the home page's hero asks for it there, where the
    /// header hides its own copy.
    /// </summary>
    [Parameter] public bool RegisterShortcut { get; set; }


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (RegisterShortcut is false || _shortcutRegistered) return;

        _shortcutRegistered = true;

        await JSRuntime.RegisterSearchShortcut(_rootId);
    }


    /// <summary>
    /// The catalog ranks the matches (see <see cref="ComponentCatalog.Search"/>) and this hands the
    /// best of them to the callout, which is why the box asks for a provider rather than filtering a
    /// fixed list: a filter can only say whether a component matches, and the whole point here is
    /// that "tag" puts Tag above TagsInput and "select" puts Dropdown above the components that
    /// merely mention selection. Synchronous - the catalog is ~110 items already in memory, so the
    /// callout never has to show a loading state.
    /// </summary>
    private static ValueTask<IEnumerable<string>> Suggest(BitSearchBoxSuggestItemsProviderRequest request)
    {
        var matches = ComponentCatalog.Search(request.SearchTerm, request.Take);

        return ValueTask.FromResult<IEnumerable<string>>([.. matches.Select(i => i.Name)]);
    }

    private void GoToComponent(string name)
    {
        if (_byName.TryGetValue(name, out var item) is false) return;

        NavigationManager.NavigateTo(item.Url);
    }

    /// <summary>
    /// Enter without picking a suggestion. The single best match is what the reader almost always
    /// meant; anything less certain hands them the gallery with the term already applied, which is a
    /// results page rather than a dead end.
    /// </summary>
    private void HandleSearch(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        var matches = ComponentCatalog.Search(term);

        // The best match leads, so the name typed in full is the first of them when it is there at
        // all, and there is no second list to search for it.
        if (matches.Count > 0)
        {
            var best = matches[0];

            if (matches.Count == 1 || string.Equals(best.Name, term.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                NavigationManager.NavigateTo(best.Url);
                return;
            }
        }

        NavigationManager.NavigateTo($"/components?q={Uri.EscapeDataString(term.Trim())}");
    }
}
