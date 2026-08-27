namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One component as the documentation site talks about it: the gallery card, the home page list and
/// the prev/next pager on a demo page are all the same record rendered three ways.
/// </summary>
public sealed class ComponentCatalogItem
{
    /// <summary>The score <see cref="Relevance"/> gives a component the term does not match at all.</summary>
    public const int NoMatch = int.MaxValue;

    // The lower-cased forms the ranking compares against, built on the first search rather than in
    // the catalog's static constructor: every page builds the catalog, only the search boxes rank it.
    private string? _nameKey;
    private string[]? _secondaryKeys;
    private string? _keywordsKey;


    /// <summary>The component's display name, without the Bit prefix (e.g. "DatePicker").</summary>
    public required string Name { get; init; }

    /// <summary>The demo page's route (e.g. "/components/datepicker").</summary>
    public required string Url { get; init; }

    /// <summary>The category the nav groups it under (e.g. "Inputs").</summary>
    public required string Category { get; init; }

    /// <summary>
    /// The names the component is also known by in other libraries ("Select, ComboBox"), taken from
    /// the nav item so the gallery search finds a component by the name the reader already knows.
    /// </summary>
    public string? Aliases { get; init; }

    /// <summary>
    /// The extra words the nav entry tags the component with ("Chips", "AutoComplete"): things the
    /// component does rather than names it goes by.
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>One line on what the component is for. Shown on the gallery card.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Everything the gallery's search box matches against, lower-cased once at build time.</summary>
    public string SearchText { get; init; } = string.Empty;


    /// <summary>
    /// How closely this component answers a search term: the lower the score the better the match,
    /// and <see cref="NoMatch"/> when the term does not match it at all.
    /// <para>
    /// Someone typing a name is looking for the component that carries it, so every hit in the name
    /// outranks every hit in the secondary names, and those in turn outrank the parts that only
    /// describe the component - its keywords, its category and its summary. Within each of those
    /// tiers the whole name beats a prefix of it and a prefix beats a hit in the middle, which is
    /// what puts Tag above TagsInput, and TagsInput above the components merely tagged with it.
    /// </para>
    /// </summary>
    /// <param name="term">The search term, already trimmed and lower-cased.</param>
    public int Relevance(string term)
    {
        if (term.Length == 0) return 0;

        _nameKey ??= Name.ToLowerInvariant();

        if (Rank(_nameKey, term) is int name) return name;

        // The aliases arrive as one string ("Select, ComboBox"), and a reader types one of the names
        // in it, not the list, so they are compared one by one.
        _secondaryKeys ??= string.IsNullOrWhiteSpace(Aliases)
            ? []
            : [.. Aliases.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        var secondary = NoMatch;
        foreach (var key in _secondaryKeys)
        {
            if (Rank(key, term) is int rank && rank < secondary) secondary = rank;
        }

        if (secondary != NoMatch) return 3 + secondary;

        _keywordsKey ??= Keywords?.ToLowerInvariant() ?? string.Empty;

        if (_keywordsKey.Contains(term, StringComparison.Ordinal)) return 6;

        // The last tier is the catalog's own haystack rather than the category and the summary read
        // separately, so that what this ranks and what the gallery's plain filter matches stay the
        // same set of components, ordered here and left in nav order there.
        return SearchText.Contains(term, StringComparison.Ordinal) ? 7 : NoMatch;
    }

    /// <summary>
    /// Where a term sits in one candidate name - 0 for the whole of it, 1 for its start, 2 for
    /// anywhere inside it - or null when it is not in there at all.
    /// </summary>
    private static int? Rank(string candidate, string term)
    {
        if (string.Equals(candidate, term, StringComparison.Ordinal)) return 0;

        if (candidate.StartsWith(term, StringComparison.Ordinal)) return 1;

        return candidate.Contains(term, StringComparison.Ordinal) ? 2 : null;
    }
}
