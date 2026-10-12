using System.Collections.Concurrent;

namespace Bit.BlazorUI;

/// <summary>
/// The country list a country selector opens, shared by every component that offers one (<see cref="BitPhoneInput"/>,
/// <see cref="BitCountryPicker"/>): the countries it offers, what a search term filters them down to, the active
/// one the keyboard is on and the type-ahead that moves it. The component keeps what is its own - when the list
/// opens and closes, where the focus goes and what a choice does - and asks this for everything else, so the two
/// walk, search and type into their lists the same way and a fix to how they do it is made once.
/// </summary>
internal sealed class BitCountryList
{
    /// <summary>
    /// How far PageDown and PageUp move the active country.
    /// </summary>
    internal const int PageSize = 10;

    // Keys whose default browser behavior the selector suppresses. They are applied through a deterministic JS
    // keydown listener (see BitExtrasSetPreventKeys) so the suppression always matches the key of the current event
    // instead of lagging one event behind, as Blazor's stateful `@onkeydown:preventDefault` binding would.
    // Home and End are deliberately absent from the search box set: inside a text field they move the caret, which
    // is what a user typing a search term expects them to keep doing. Enter and Space are left alone on the closed
    // trigger, whose native click is what opens the list; on the open one they pick the active country, and the click
    // they would also fire would close the list again.
    internal static readonly string[] SearchBoxKeys = ["ArrowDown", "ArrowUp", "PageDown", "PageUp", "Enter"];
    internal static readonly string[] TriggerClosedKeys = ["ArrowDown", "ArrowUp", "Home", "End"];
    internal static readonly string[] TriggerOpenKeys = ["ArrowDown", "ArrowUp", "PageDown", "PageUp", "Home", "End", "Enter", " ", "Spacebar"];

    private static readonly TimeSpan _typeAheadTimeout = TimeSpan.FromMilliseconds(1000);

    // The packaged flag file of a country, keyed by its ISO code, or null for a code that has none. Every open list
    // renders one image per country it offers, and the url of each of them is otherwise built from scratch on every
    // render.
    private static readonly ConcurrentDictionary<string, string?> _packagedFlagUrls = new(StringComparer.OrdinalIgnoreCase);

    private readonly Func<string, string>? _codeTermOf;
    private readonly Func<BitCountry, int>? _dialCodeOwnerRank;

    private ICollection<BitCountry>? _lastCountries;
    private ICollection<BitCountry>? _lastExcludeCountries;
    private ICollection<BitCountry>? _lastPreferredCountries;

    // How many of the countries at the head of All are there because they were preferred, which is where the
    // unfiltered list draws the rule that sets them apart from the rest.
    private int _preferredCount;

    // The name of each country of All with its diacritics folded away, and the same name cut into words. Both are
    // read once per country per keystroke, so they are computed with the list instead of being rebuilt for every
    // country on every letter typed into the search box.
    private string[] _foldedNames = [];
    private string[][] _foldedWords = [];

    // The search term the cached View was produced for. The view is read on every render while the list is open,
    // so it is only rebuilt when the term (or the source list behind it) actually changes.
    private bool _viewValid;
    private string? _viewKey;

    private List<BitCountry> _view = [];

    private string _typeAhead = string.Empty;
    private DateTimeOffset _typeAheadAt;



    /// <summary>
    /// A list searched by the names and the ISO codes of its countries.
    /// </summary>
    internal BitCountryList() { }

    /// <summary>
    /// A list searched by the dialing codes of its countries as well: <paramref name="codeTermOf"/> reduces a folded
    /// term to the digits of the code it stands for (empty for none), and <paramref name="dialCodeOwnerRank"/> orders
    /// the countries sharing a code, higher first.
    /// </summary>
    internal BitCountryList(Func<string, string> codeTermOf, Func<BitCountry, int> dialCodeOwnerRank)
    {
        _codeTermOf = codeTermOf;
        _dialCodeOwnerRank = dialCodeOwnerRank;
    }



    /// <summary>
    /// Every country the list offers, in the order it shows them unfiltered.
    /// </summary>
    internal List<BitCountry> All { get; private set; } = [];

    /// <summary>
    /// The countries the current search term leaves, in the order the list shows them.
    /// </summary>
    internal List<BitCountry> View
    {
        get
        {
            Filter();
            return _view;
        }
    }

    /// <summary>
    /// The search term the list is filtered by.
    /// </summary>
    internal string? SearchText { get; private set; }

    /// <summary>
    /// The index in <see cref="View"/> of the country the keyboard is on, or -1 for none.
    /// </summary>
    internal int ActiveIndex { get; set; } = -1;

    /// <summary>
    /// The index of the country last scrolled into view, so it is only scrolled to once per move.
    /// </summary>
    internal int LastScrolledIndex { get; set; } = -1;

    /// <summary>
    /// The country the keyboard is on, or null for none.
    /// </summary>
    internal BitCountry? Active => ActiveIndex >= 0 && ActiveIndex < View.Count ? View[ActiveIndex] : null;

    /// <summary>
    /// Whether a type-ahead term is still being typed, which is when a space is part of it rather than a choice.
    /// </summary>
    internal bool IsTypeAheadInProgress => _typeAhead.Length > 0 && (DateTimeOffset.UtcNow - _typeAheadAt) <= _typeAheadTimeout;



    /// <summary>
    /// Builds the list again when the collections it is built from are not the ones it was last built from.
    /// </summary>
    internal void Update(ICollection<BitCountry> countries, ICollection<BitCountry>? excludeCountries, ICollection<BitCountry>? preferredCountries)
    {
        if (ReferenceEquals(_lastCountries, countries) &&
            ReferenceEquals(_lastExcludeCountries, excludeCountries) &&
            ReferenceEquals(_lastPreferredCountries, preferredCountries)) return;

        _lastCountries = countries;
        _lastExcludeCountries = excludeCountries;
        _lastPreferredCountries = preferredCountries;

        All = BitCountrySearch.BuildItems(countries, excludeCountries, preferredCountries, out _preferredCount);
        _foldedNames = [.. All.Select(c => BitCountrySearch.Fold(c.Name))];
        _foldedWords = [.. _foldedNames.Select(BitCountrySearch.SplitWords)];
        _viewValid = false;
    }

    /// <summary>
    /// The country of the list that carries the ISO code of the given one - the instance the list offers, with the
    /// name it shows - or null when the list does not offer it.
    /// </summary>
    internal BitCountry? Resolve(BitCountry? country)
    {
        if (country is null) return null;

        return All.Find(c => c.Iso2.Equals(country.Iso2, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the list by a new search term and puts the active country on the first match, returning how many
    /// countries the term leaves.
    /// </summary>
    internal int Search(string? text)
    {
        SearchText = text;
        _viewValid = false;

        // The first match is what Enter picks, rather than a country the term filtered away.
        ActiveIndex = View.Count > 0 ? 0 : -1;
        LastScrolledIndex = -1;

        return View.Count;
    }

    /// <summary>
    /// Forgets the search term, the active country and the type-ahead, which is the state a closed list is in.
    /// </summary>
    internal void Reset()
    {
        SearchText = null;
        ActiveIndex = -1;
        LastScrolledIndex = -1;
        _typeAhead = string.Empty;
        _viewValid = false;
    }

    /// <summary>
    /// Forgets the type-ahead term, so the next letter starts a new one.
    /// </summary>
    internal void ResetTypeAhead() => _typeAhead = string.Empty;

    /// <summary>
    /// Puts the active country on the selected one, or on the first country when none is, so the list always opens
    /// with something for Enter to act on.
    /// </summary>
    internal void ResetActiveToSelection(Func<BitCountry, bool> isSelected)
    {
        var selectedIndex = View.FindIndex(c => isSelected(c));
        ActiveIndex = selectedIndex >= 0 ? selectedIndex : (View.Count > 0 ? 0 : -1);
        LastScrolledIndex = -1;
    }

    /// <summary>
    /// Whether the country at the index is the last of the pinned ones, after which the rest of the list starts.
    /// Only the unfiltered list has such a boundary: a search orders its matches by how close they are, wherever
    /// they came from.
    /// </summary>
    internal bool IsLastPreferred(int index) => _preferredCount > 0
                                                && index == _preferredCount - 1
                                                && index < All.Count - 1
                                                && ReferenceEquals(View, All);

    /// <summary>
    /// Moves the active country for one of the keys that walk a list, and reports whether the key was one of them.
    /// Inside a search box Home and End belong to the caret, so they move nothing when that is where they were
    /// pressed.
    /// </summary>
    internal bool Navigate(string key, bool fromSearchBox, bool noWrap)
    {
        switch (key)
        {
            case "ArrowDown":
                Move(1, clamp: noWrap);
                return true;

            case "ArrowUp":
                Move(-1, clamp: noWrap);
                return true;

            case "PageDown":
                Move(PageSize, clamp: true);
                return true;

            case "PageUp":
                Move(-PageSize, clamp: true);
                return true;

            case "Home":
                if (fromSearchBox is false && View.Count > 0) ActiveIndex = 0;
                return true;

            case "End":
                if (fromSearchBox is false && View.Count > 0) ActiveIndex = View.Count - 1;
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Moves the active country to the next one whose name starts with what was typed, the way a native select
    /// behaves.
    /// </summary>
    internal void TypeAhead(string key)
    {
        var view = View;

        if (view.Count == 0) return;

        var now = DateTimeOffset.UtcNow;

        // A pause longer than the timeout starts a new term, and repeating a single letter cycles through the
        // countries starting with it instead of searching for a doubled letter.
        _typeAhead = (now - _typeAheadAt) > _typeAheadTimeout ? key : _typeAhead + key;
        _typeAheadAt = now;

        var term = BitCountrySearch.Fold(_typeAhead);
        var start = _typeAhead.Length == 1 ? ActiveIndex + 1 : Math.Max(ActiveIndex, 0);

        // The type-ahead runs on the unfiltered list (there is no search box to filter it with), so the names folded
        // once with the list are the same ones this walks over.
        var folded = ReferenceEquals(view, All) ? _foldedNames : null;

        var index = BitCountrySearch.FindByNameStart(view, folded, term, start);

        if (index >= 0)
        {
            ActiveIndex = index;
        }
    }

    /// <summary>
    /// A printable character typed without a modifier: a shortcut (Ctrl+C, Alt+letter) is not a letter of a name.
    /// </summary>
    internal static bool IsTypeAheadKey(KeyboardEventArgs e) => e.Key.Length == 1
                                                                && char.IsLetterOrDigit(e.Key[0])
                                                                && e.CtrlKey is false
                                                                && e.AltKey is false
                                                                && e.MetaKey is false;

    /// <summary>
    /// The url of the flag image of a country: the one the selector returns, else the packaged one, else null for a
    /// country that has none of either - a custom one whose code no flag ships for - which is drawn as an empty box
    /// rather than an image that fails to load.
    /// </summary>
    internal static string? GetFlagUrl(BitCountry country, Func<BitCountry, string?>? selector)
    {
        var url = selector?.Invoke(country);

        if (url.HasValue()) return url;

        // Where the flag images live and how they are named is BitFlag's to know, so the two cannot drift. A code is
        // looked up the way BitFlag looks it up (BitCountries.HasFlag), which is also what files the exceptionally
        // reserved "UK" under the "GB" its flag ships as.
        return _packagedFlagUrls.GetOrAdd(country.Iso2, static iso2 => BitCountries.FindByIso2(iso2) is { } known ? BitFlag.GetFlagUrl(known.Iso2) : null);
    }

    /// <summary>
    /// What a screen reader is told about the countries a search term leaves.
    /// </summary>
    internal static string? GetSearchAnnouncement(int count, string? noResultsMessage, Func<int, string?>? announcement) => count switch
    {
        0 => noResultsMessage ?? "No results found",
        _ when announcement is not null => announcement(count),
        1 => "1 country found",
        _ => $"{count} countries found"
    };



    private void Move(int offset, bool clamp)
    {
        var count = View.Count;

        if (count == 0) return;

        var next = ActiveIndex < 0 ? (offset > 0 ? 0 : count - 1) : ActiveIndex + offset;

        if (clamp)
        {
            next = Math.Clamp(next, 0, count - 1);
        }
        else if (next < 0)
        {
            next = count - 1;
        }
        else if (next > count - 1)
        {
            next = 0;
        }

        ActiveIndex = next;
    }

    private void Filter()
    {
        if (_viewValid && _viewKey == SearchText) return;

        _viewValid = true;
        _viewKey = SearchText;

        _view = FilterCore();
    }

    private List<BitCountry> FilterCore()
    {
        if (SearchText.HasNoValue()) return All;

        var text = BitCountrySearch.Fold(SearchText!.Trim());

        // A dialing code is searched for the way it is written down, which the selector that searches codes reduces
        // to its digits; one that does not passes no code term, which leaves the codes out of the search.
        var codeTerm = _codeTermOf?.Invoke(text) ?? string.Empty;

        // A term that is nothing but punctuation and spaces asks for nothing - the '+' of a dialing code about to be
        // typed is a term of that kind - and answering it with "no results" would say the country being typed does
        // not exist.
        if (codeTerm.Length == 0 && text.Any(char.IsLetterOrDigit) is false) return All;

        var matches = new List<(BitCountry Country, int Rank, int Index)>();

        for (var i = 0; i < All.Count; i++)
        {
            var rank = BitCountrySearch.GetSearchRank(All[i], _foldedNames[i], _foldedWords[i], text, codeTerm);
            if (rank < 0) continue;

            matches.Add((All[i], rank, i));
        }

        // The closest match first (an exact ISO code, then a name that starts with the term, then a dialing code that
        // does), with the list order kept inside each of those groups. A group of countries matched by their dialing
        // code is ordered by the owner of the code, so searching "+1" offers the United States before Canada rather
        // than whichever of them the alphabet happens to put first.
        matches.Sort((x, y) =>
        {
            if (x.Rank != y.Rank) return x.Rank.CompareTo(y.Rank);

            if (_dialCodeOwnerRank is not null && BitCountrySearch.IsDialCodeRank(x.Rank))
            {
                var byOwner = _dialCodeOwnerRank(y.Country).CompareTo(_dialCodeOwnerRank(x.Country));
                if (byOwner != 0) return byOwner;
            }

            return x.Index.CompareTo(y.Index);
        });

        return [.. matches.Select(m => m.Country)];
    }
}
