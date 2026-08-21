namespace Bit.BlazorUI.Demo.Client.Core.Components;

/// <summary>
/// The components index: a searchable, category-filtered grid over <see cref="ComponentCatalog"/>.
/// <para>
/// It replaces a hand-written list of ~110 links that had to be edited every time a component was
/// added, could not be searched, and said nothing about what any of them were for.
/// </para>
/// </summary>
public partial class ComponentGallery
{
    private string? _filter;
    private string? _category;
    private List<ComponentCatalogCategory> _groups = [];


    /// <summary>
    /// A term to start filtered by - the header's search box hands one over when Enter did not pick
    /// out a single component (see <see cref="ComponentSearchBox"/>).
    /// </summary>
    [Parameter] public string? InitialFilter { get; set; }


    protected override Task OnParamsSetAsync()
    {
        // Only while the reader has not touched the box themselves: a later navigation to the same
        // page with a different term should re-filter, but a re-render must not undo their typing.
        if (_filter is null && InitialFilter.HasValue())
        {
            _filter = InitialFilter;
        }

        Rebuild();

        return base.OnParamsSetAsync();
    }


    private void ApplyFilter(string? value)
    {
        _filter = value;
        Rebuild();
    }

    private void SelectCategory(string? category)
    {
        // Clicking the category you are already in clears it, which is what a pressed toggle implies.
        _category = _category == category ? null : category;
        Rebuild();
    }

    private void ClearFilters()
    {
        _filter = null;
        _category = null;
        Rebuild();
    }

    /// <summary>
    /// Rebuilds the visible groups from the current filter and category. Runs over ~110 prebuilt
    /// strings, so it is cheap enough to do on every keystroke without a debounce of its own (the
    /// search box already has one).
    /// </summary>
    private void Rebuild()
    {
        var term = _filter?.Trim().ToLowerInvariant();

        _groups = [];

        foreach (var category in ComponentCatalog.Categories)
        {
            if (_category is not null && category.Name != _category) continue;

            var items = term.HasValue()
                ? category.Items.Where(i => i.SearchText.Contains(term!, StringComparison.Ordinal)).ToArray()
                : [.. category.Items];

            if (items.Length == 0) continue;

            _groups.Add(new ComponentCatalogCategory
            {
                Name = category.Name,
                IconName = category.IconName,
                Summary = category.Summary,
                Items = items
            });
        }
    }
}
