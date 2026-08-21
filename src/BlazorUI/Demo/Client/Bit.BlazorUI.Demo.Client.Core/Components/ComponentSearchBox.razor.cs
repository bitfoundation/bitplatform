namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// The header's component finder: type a few characters, get the matching components with the
/// category they belong to, press Enter or click to go there.
/// <para>
/// It searches <see cref="ComponentCatalog"/>, so it matches on more than the visible name - the
/// aliases another library would use ("Select", "Switch", "Chip"), the category, and the one-line
/// summary. Someone who knows what they want but not what bit calls it still lands on the page.
/// </para>
/// </summary>
public partial class ComponentSearchBox
{
    /// <summary>
    /// The suggestion list is the component names; everything else the search matches on is reached
    /// through the catalog by name (see <see cref="Matches"/>). Built once, statically: the list is
    /// the same on every page and for every instance.
    /// </summary>
    private static readonly string[] _names = [.. ComponentCatalog.Items.Select(i => i.Name)];

    private static readonly Dictionary<string, ComponentCatalogItem> _byName =
        ComponentCatalog.Items.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ties the keyboard shortcut to this instance's input. Unique per instance because the header
    /// and the gallery can both be on screen; only the first one registered claims the shortcut.
    /// </summary>
    private readonly string _rootId = $"cmp-search-{Guid.NewGuid():N}";

    private bool _shortcutRegistered;


    [Parameter] public string? Class { get; set; }

    [Parameter] public string Placeholder { get; set; } = "Search components";

    /// <summary>
    /// Whether Ctrl/Cmd+K should put the caret in this box. Only the header's copy asks for it.
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
    /// The catalog's prebuilt search text rather than the name alone, so "select" finds Dropdown and
    /// "skeleton" finds Shimmer. Falls back to a plain name match for a suggestion the catalog has
    /// no entry for, which cannot happen today but keeps the box working if it ever does.
    /// </summary>
    private static bool Matches(string? search, string? name)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        if (name is null) return false;

        var term = search.Trim().ToLowerInvariant();

        return _byName.TryGetValue(name, out var item)
            ? item.SearchText.Contains(term, StringComparison.Ordinal)
            : name.Contains(term, StringComparison.OrdinalIgnoreCase);
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

        var matches = ComponentCatalog.Items.Where(i => Matches(term, i.Name)).ToArray();

        var exact = matches.FirstOrDefault(i => string.Equals(i.Name, term.Trim(), StringComparison.OrdinalIgnoreCase));

        if (exact is not null || matches.Length == 1)
        {
            NavigationManager.NavigateTo((exact ?? matches[0]).Url);
            return;
        }

        NavigationManager.NavigateTo($"/components?q={Uri.EscapeDataString(term.Trim())}");
    }
}
