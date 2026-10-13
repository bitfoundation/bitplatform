using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The time zone list <see cref="BitTimeZonePicker{TValue}"/> opens: the time zones it offers in the order of their
/// offsets from UTC, what a search term filters them down to, the active one the keyboard is on and the type-ahead that
/// moves it. It walks and is typed into the way <see cref="BitCountryList"/> is, whose keys it shares, so the two
/// pickers answer a keyboard the same way.
/// </summary>
internal sealed class BitTimeZoneList
{
    // The keys the field prevents: the ones of BitCountryList, plus the two Firefox opens its quick find bar on while the
    // focus is on something that takes no text - a slash and an apostrophe, which a time zone search is typed with
    // ("Asia/", "+05:30") and which a key typed on the field hands to the search box.
    internal static readonly string[] TriggerClosedKeys = [.. BitCountryList.TriggerClosedKeys, "/", "'"];
    internal static readonly string[] TriggerOpenKeys = [.. BitCountryList.TriggerOpenKeys, "/", "'"];

    private static readonly TimeSpan _typeAheadTimeout = TimeSpan.FromMilliseconds(1000);

    private ICollection<BitTimeZone>? _lastTimeZones;
    private ICollection<string>? _lastPreferredTimeZones;
    private DateTimeOffset? _lastReferenceDate;

    // The hour the offsets were computed in while no reference date is given: a clock change happens on an hour, so the
    // offsets - and the order they give the list - are computed again once the hour has moved on.
    private long _offsetsHour = long.MinValue;

    // How many of the time zones at the head of All are there because they were preferred, which is where the
    // unfiltered list draws the rule that sets them apart from the rest.
    private int _preferredCount;

    private Entry[] _entries = [];

    // The offset of each time zone of All, which every row of an open list reads on every render.
    private Dictionary<BitTimeZone, TimeSpan> _offsets = new(ReferenceEqualityComparer.Instance);

    // The search term the cached View was produced for. The view is read on every render while the list is open, so it
    // is only rebuilt when the term (or the source list behind it) actually changes.
    private bool _viewValid;
    private string? _viewKey;

    private List<BitTimeZone> _view = [];

    private string _typeAhead = string.Empty;
    private DateTimeOffset _typeAheadAt;



    /// <summary>
    /// Every time zone the list offers, in the order it shows them unfiltered.
    /// </summary>
    internal List<BitTimeZone> All { get; private set; } = [];

    /// <summary>
    /// The time zones the current search term leaves, in the order the list shows them.
    /// </summary>
    internal List<BitTimeZone> View
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
    /// The index in <see cref="View"/> of the time zone the keyboard is on, or -1 for none.
    /// </summary>
    internal int ActiveIndex { get; set; } = -1;

    /// <summary>
    /// The index of the time zone last scrolled into view, so it is only scrolled to once per move.
    /// </summary>
    internal int LastScrolledIndex { get; set; } = -1;

    /// <summary>
    /// The time zone the keyboard is on, or null for none.
    /// </summary>
    internal BitTimeZone? Active => ActiveIndex >= 0 && ActiveIndex < View.Count ? View[ActiveIndex] : null;

    /// <summary>
    /// Whether a type-ahead term is still being typed, which is when a space is part of it rather than a choice.
    /// </summary>
    internal bool IsTypeAheadInProgress => _typeAhead.Length > 0 && (DateTimeOffset.UtcNow - _typeAheadAt) <= _typeAheadTimeout;



    /// <summary>
    /// Builds the list again when the collections it is built from are not the ones it was last built from, or when
    /// the moment its offsets are computed for has moved on.
    /// </summary>
    internal void Update(ICollection<BitTimeZone> timeZones, ICollection<string>? preferredTimeZones, DateTimeOffset? referenceDate)
    {
        var hour = referenceDate.HasValue ? long.MinValue : DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600;

        if (ReferenceEquals(_lastTimeZones, timeZones) &&
            ReferenceEquals(_lastPreferredTimeZones, preferredTimeZones) &&
            _lastReferenceDate == referenceDate &&
            _offsetsHour == hour) return;

        _lastTimeZones = timeZones;
        _lastPreferredTimeZones = preferredTimeZones;
        _lastReferenceDate = referenceDate;
        _offsetsHour = hour;

        Build(timeZones, preferredTimeZones, referenceDate ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// The time zone of the list an id stands for - an IANA id, an older one, a Windows id - or null when the list does
    /// not offer it.
    /// </summary>
    internal BitTimeZone? Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var canonical = CanonicalId(id.Trim());

        return All.Find(z => string.Equals(CanonicalId(z.Id), canonical, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The offset from UTC the list shows for one of its time zones, which is the one it is ordered by.
    /// </summary>
    internal TimeSpan GetOffset(BitTimeZone zone)
    {
        if (_offsets.TryGetValue(zone, out var offset)) return offset;

        return zone.GetUtcOffset(_lastReferenceDate ?? DateTimeOffset.UtcNow) ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Filters the list by a new search term and puts the active time zone on the first match, returning how many time
    /// zones the term leaves.
    /// </summary>
    internal int Search(string? text)
    {
        SearchText = text;
        _viewValid = false;

        // The first match is what Enter picks, rather than a time zone the term filtered away.
        ActiveIndex = View.Count > 0 ? 0 : -1;
        LastScrolledIndex = -1;

        return View.Count;
    }

    /// <summary>
    /// Forgets the search term, the active time zone and the type-ahead, which is the state a closed list is in.
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
    /// Puts the active time zone on the selected one, or on the first time zone when none is, so the list always opens
    /// with something for Enter to act on.
    /// </summary>
    internal void ResetActiveToSelection(Func<BitTimeZone, bool> isSelected)
    {
        var selectedIndex = View.FindIndex(z => isSelected(z));
        ActiveIndex = selectedIndex >= 0 ? selectedIndex : (View.Count > 0 ? 0 : -1);
        LastScrolledIndex = -1;
    }

    /// <summary>
    /// Whether the time zone at the index is the last of the pinned ones, after which the rest of the list starts. Only
    /// the unfiltered list has such a boundary: a search orders its matches by how close they are, wherever they came
    /// from.
    /// </summary>
    internal bool IsLastPreferred(int index) => _preferredCount > 0
                                                && index == _preferredCount - 1
                                                && index < All.Count - 1
                                                && ReferenceEquals(View, All);

    /// <summary>
    /// Moves the active time zone for one of the keys that walk a list, and reports whether the key was one of them.
    /// Inside a search box Home and End belong to the caret, so they move nothing when that is where they were pressed.
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
                Move(BitCountryList.PageSize, clamp: true);
                return true;

            case "PageUp":
                Move(-BitCountryList.PageSize, clamp: true);
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
    /// Moves the active time zone to the next one whose name starts with what was typed, the way a native select
    /// behaves.
    /// </summary>
    internal void TypeAhead(string key)
    {
        var view = View;

        if (view.Count == 0) return;

        var now = DateTimeOffset.UtcNow;

        // A pause longer than the timeout starts a new term, and repeating a single letter cycles through the time zones
        // starting with it instead of searching for a doubled letter.
        _typeAhead = (now - _typeAheadAt) > _typeAheadTimeout ? key : _typeAhead + key;
        _typeAheadAt = now;

        var term = BitCountrySearch.Fold(_typeAhead);
        var start = _typeAhead.Length == 1 ? ActiveIndex + 1 : Math.Max(ActiveIndex, 0);

        for (var i = 0; i < view.Count; i++)
        {
            var index = (start + i) % view.Count;
            if (index < 0) index += view.Count;

            if (FoldedNameOf(view, index).StartsWith(term, StringComparison.InvariantCultureIgnoreCase))
            {
                ActiveIndex = index;
                return;
            }
        }
    }

    /// <summary>
    /// A character of a search term typed without a modifier. Unlike a type-ahead, which only ever matches the letters
    /// of a name, a search is typed with punctuation too ("+03:30", "Asia/"), and a key typed on the field before the
    /// focus has reached the search box - the field is where the keys land in the moment the list takes to open - is
    /// handed on to it, so none of those may be lost on the way. A space is left out: on the field it is the native click.
    /// </summary>
    internal static bool IsSearchKey(KeyboardEventArgs e) => e.Key.Length == 1
                                                             && char.IsWhiteSpace(e.Key[0]) is false
                                                             && e.CtrlKey is false
                                                             && e.AltKey is false
                                                             && e.MetaKey is false;

    /// <summary>
    /// What a screen reader is told about the time zones a search term leaves.
    /// </summary>
    internal static string? GetSearchAnnouncement(int count, string? noResultsMessage, Func<int, string?>? announcement) => count switch
    {
        0 => noResultsMessage ?? "No results found",
        _ when announcement is not null => announcement(count),
        1 => "1 time zone found",
        _ => $"{count} time zones found"
    };



    // The id of the table an id stands for, so an alias, a Windows id and a canonical id of the same zone compare equal;
    // an id the table does not know stands for itself.
    private static string CanonicalId(string id) => BitTimeZones.Find(id)?.Id ?? id;

    private string FoldedNameOf(List<BitTimeZone> view, int index)
    {
        // The type-ahead runs on the unfiltered list (there is no search box to filter it with), so the names folded once
        // with the list are the same ones it walks over.
        return ReferenceEquals(view, All) ? _entries[index].Name : BitCountrySearch.Fold(view[index].Name);
    }

    private void Build(ICollection<BitTimeZone> timeZones, ICollection<string>? preferredTimeZones, DateTimeOffset at)
    {
        // A time zone is offered once, by the first entry of the list that stands for it: its canonical id is what tells a
        // row apart from the others (its key, its selected state), and a list that names a zone twice - an alias beside
        // the canonical id, or a translated entry beside the built-in one - would otherwise render two rows nothing could
        // tell apart. A time zone the runtime knows nothing of is left out, since it has no offset to show nor a
        // TimeZoneInfo to hand back.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<Entry>(timeZones.Count);

        foreach (var zone in timeZones)
        {
            if (zone is null || seen.Add(CanonicalId(zone.Id)) is false) continue;

            if (zone.GetUtcOffset(at) is not { } offset) continue;

            entries.Add(new Entry(zone, offset));
        }

        // West to east, the way a map and every other time zone list reads, and by name within an offset.
        entries.Sort(static (x, y) =>
        {
            var byOffset = x.Offset.CompareTo(y.Offset);

            return byOffset != 0 ? byOffset : string.Compare(x.Zone.Name, y.Zone.Name, StringComparison.CurrentCultureIgnoreCase);
        });

        _preferredCount = 0;

        if (preferredTimeZones is not null && preferredTimeZones.Count > 0)
        {
            var preferred = new List<Entry>();
            var preferredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in preferredTimeZones)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;

                var canonical = CanonicalId(id.Trim());

                // Only the time zones the list actually offers can be pinned to the top of it.
                var index = entries.FindIndex(e => string.Equals(CanonicalId(e.Zone.Id), canonical, StringComparison.OrdinalIgnoreCase));
                if (index < 0 || preferredIds.Add(canonical) is false) continue;

                preferred.Add(entries[index]);
            }

            _preferredCount = preferred.Count;

            entries = [.. preferred, .. entries.Where(e => preferredIds.Contains(CanonicalId(e.Zone.Id)) is false)];
        }

        _entries = [.. entries];
        _offsets = _entries.ToDictionary<Entry, BitTimeZone, TimeSpan>(e => e.Zone, e => e.Offset, ReferenceEqualityComparer.Instance);
        All = [.. entries.Select(e => e.Zone)];
        _viewValid = false;
    }

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

    private List<BitTimeZone> FilterCore()
    {
        if (SearchText.HasNoValue()) return All;

        var text = BitCountrySearch.Fold(SearchText!.Trim());

        var offsetTerm = OffsetTerm.Parse(text);

        // A term that is nothing but punctuation and spaces asks for nothing - the '+' of an offset about to be typed is a
        // term of that kind - and answering it with "no results" would say the time zone being typed does not exist.
        if (offsetTerm is null && text.Any(char.IsLetterOrDigit) is false) return All;

        // A term that is an id the table knows (an alias, a Windows id, a spelling of UTC) finds its zone first.
        var exactId = BitTimeZones.Find(text)?.Id;

        var matches = new List<(BitTimeZone Zone, int Rank, int Index)>();

        for (var i = 0; i < _entries.Length; i++)
        {
            var rank = _entries[i].GetSearchRank(text, exactId, offsetTerm);
            if (rank < 0) continue;

            matches.Add((_entries[i].Zone, rank, i));
        }

        // The closest match first, with the list order - by offset - kept inside each group of matches.
        matches.Sort(static (x, y) => x.Rank != y.Rank ? x.Rank.CompareTo(y.Rank) : x.Index.CompareTo(y.Index));

        return [.. matches.Select(m => m.Zone)];
    }



    // A time zone of the list with its offset and the texts a search is matched against, folded once with the list
    // rather than for every zone on every letter typed into the search box.
    private sealed class Entry
    {
        internal Entry(BitTimeZone zone, TimeSpan offset)
        {
            Zone = zone;
            Offset = offset;
            Name = BitCountrySearch.Fold(zone.Name);
            Words = BitCountrySearch.SplitWords(Name);
            Country = zone.CountryName is { Length: > 0 } country ? BitCountrySearch.Fold(country) : string.Empty;
            Id = zone.Id.Replace('_', ' ');
            WindowsId = zone.WindowsId ?? string.Empty;
        }

        internal BitTimeZone Zone { get; }

        internal TimeSpan Offset { get; }

        internal string Name { get; }

        internal string[] Words { get; }

        internal string Country { get; }

        internal string Id { get; }

        internal string WindowsId { get; }

        // The rank of the time zone against a search term, lower being closer, or a negative one where it does not match
        // the term at all.
        internal int GetSearchRank(string text, string? exactId, OffsetTerm? offsetTerm)
        {
            if (exactId is not null && string.Equals(CanonicalId(Zone.Id), exactId, StringComparison.OrdinalIgnoreCase)) return 0;
            if (offsetTerm is { } term) return term.Matches(Offset) ? 1 : -1;
            if (Name.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) return 2;
            if (Country.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) return 3;

            // A term of several words matches a name whose words start with them ("sao pa"), or a place written with its
            // country after it ("paris fr").
            if (MatchesWordStarts(text)) return 4;

            // The Windows id carries the name a region is known by ("Pacific Standard Time", "Central Europe Standard
            // Time"), which is how a term like "pacific" or "central europe" finds every zone of it.
            if (WindowsId.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) return 5;
            if (Name.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 6;
            if (Country.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 7;
            if (Id.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 8;
            if (WindowsId.Contains(text, StringComparison.InvariantCultureIgnoreCase)) return 9;

            return -1;
        }

        private bool MatchesWordStarts(string text)
        {
            if (text.Contains(' ') is false) return false;

            var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (terms.Length == 0) return false;

            if (terms.Length <= Words.Length && StartsWith(Words, terms)) return true;

            // The words of the name followed by the words of the country.
            var countryWords = BitCountrySearch.SplitWords(Country);

            return terms.Length <= Words.Length + countryWords.Length && StartsWith([.. Words, .. countryWords], terms);
        }

        private static bool StartsWith(string[] words, string[] terms)
        {
            for (var i = 0; i < terms.Length; i++)
            {
                if (words[i].StartsWith(terms[i], StringComparison.InvariantCultureIgnoreCase) is false) return false;
            }

            return true;
        }
    }

    // An offset from UTC as a search term is written: "+3:30", "-05:00", "utc+3", "GMT-7", "+0530", "utc 5". A term
    // without a sign or a prefix ("3") is a name rather than an offset, and one without minutes ("+5") finds every zone
    // of that hour (+05:00, +05:30 and +05:45).
    internal readonly struct OffsetTerm
    {
        private readonly int _sign;
        private readonly int _hours;
        private readonly int? _minutes;

        private OffsetTerm(int sign, int hours, int? minutes)
        {
            _sign = sign;
            _hours = hours;
            _minutes = minutes;
        }

        internal static OffsetTerm? Parse(string text)
        {
            var term = text.Replace(" ", string.Empty).ToLowerInvariant();

            var hasPrefix = term.StartsWith("utc", StringComparison.Ordinal) || term.StartsWith("gmt", StringComparison.Ordinal);

            if (hasPrefix)
            {
                term = term[3..];
            }

            // The minus sign of typography is read like the hyphen a keyboard types.
            var sign = 0;
            if (term.Length > 0 && term[0] is '+' or '-' or '−')
            {
                sign = term[0] == '+' ? 1 : -1;
                term = term[1..];
            }

            if (hasPrefix is false && sign == 0) return null;

            // "utc" and "gmt" on their own are the names of UTC, which the exact id match finds; "+" on its own asks for
            // nothing yet.
            if (term.Length == 0) return null;

            string hoursText;
            string? minutesText = null;

            var colon = term.IndexOf(':');
            if (colon >= 0)
            {
                hoursText = term[..colon];
                minutesText = term[(colon + 1)..];

                // "+3:" is an offset still being typed, which reads as "+3" until its minutes are.
                if (minutesText.Length == 0) minutesText = null;
            }
            else if (term.Length is 3 or 4)
            {
                hoursText = term[..^2];
                minutesText = term[^2..];
            }
            else
            {
                hoursText = term;
            }

            if (hoursText.Length is 0 or > 2 || hoursText.All(char.IsAsciiDigit) is false) return null;
            if (minutesText is not null && (minutesText.Length != 2 || minutesText.All(char.IsAsciiDigit) is false)) return null;

            var hours = int.Parse(hoursText, CultureInfo.InvariantCulture);
            int? minutes = minutesText is null ? null : int.Parse(minutesText, CultureInfo.InvariantCulture);

            if (hours > 14 || minutes > 59) return null;

            return new OffsetTerm(sign == 0 ? 1 : sign, hours, minutes);
        }

        internal bool Matches(TimeSpan offset)
        {
            var offsetSign = offset < TimeSpan.Zero ? -1 : 1;
            var absolute = offset.Duration();

            // Zero is neither ahead of UTC nor behind it, so "+0" and "-0" both find it.
            if (_hours == 0 && (_minutes ?? 0) == 0 && absolute == TimeSpan.Zero) return true;

            if (offsetSign != _sign || (int)absolute.TotalHours != _hours) return false;

            return _minutes is null || absolute.Minutes == _minutes;
        }
    }
}
