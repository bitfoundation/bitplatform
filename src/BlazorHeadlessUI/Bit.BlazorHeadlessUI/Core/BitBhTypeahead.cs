namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Typeahead ("type to select") matcher used by listboxes, menus, trees and radio
/// groups. Implements the WAI-ARIA APG behaviour:
/// <list type="bullet">
///   <item>Typed characters accumulate into a search string that resets after a pause.</item>
///   <item>Typing the same character repeatedly cycles through items starting with it.</item>
///   <item>Matching is case-insensitive and skips disabled items.</item>
/// </list>
/// </summary>
public sealed class BitBhTypeahead
{
    private string _buffer = string.Empty;
    private DateTime _resetAt = DateTime.MinValue;

    /// <summary>How long after the last keystroke the search string resets. Default: 1 second.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>The current search string.</summary>
    public string Buffer => DateTime.UtcNow > _resetAt ? string.Empty : _buffer;

    /// <summary>Whether a typeahead search is in progress (Space should then extend the search).</summary>
    public bool IsActive => DateTime.UtcNow <= _resetAt && _buffer.Length > 0;

    /// <summary>
    /// Returns true when <paramref name="key"/> is a printable, non-space character
    /// that should feed typeahead. (Space only extends an active search — see <see cref="IsActive"/>.)
    /// </summary>
    public static bool IsTypeaheadKey(string? key, bool ctrlOrMeta = false, bool alt = false)
        => !ctrlOrMeta && !alt && key is { Length: 1 } && key != " " && !char.IsControl(key[0]);

    /// <summary>Clears the search string.</summary>
    public void Reset()
    {
        _buffer = string.Empty;
        _resetAt = DateTime.MinValue;
    }

    /// <summary>
    /// Appends <paramref name="key"/> to the search string and returns the index of the
    /// best match among <paramref name="count"/> items, or -1.
    /// </summary>
    public int SearchIndex(int count, string key, int currentIndex, Func<int, string?> getText, Func<int, bool> isDisabled)
    {
        var now = DateTime.UtcNow;
        if (now > _resetAt) _buffer = string.Empty;
        _buffer += key;
        _resetAt = now + Timeout;

        if (count == 0) return -1;

        // "aaa" behaves like "a" pressed three times: cycle through "a…" items.
        var repeated = _buffer.Length > 1 && _buffer.All(c => char.ToLowerInvariant(c) == char.ToLowerInvariant(_buffer[0]));
        var search = repeated ? _buffer[..1] : _buffer;

        // A single-character search starts after the current item so repeated
        // presses move forward; a longer search may stay on the current item.
        var offset = search.Length == 1 ? 1 : 0;
        var start = currentIndex < 0 ? 0 : currentIndex + offset;

        for (var i = 0; i < count; i++)
        {
            var index = (start + i) % count;
            if (isDisabled(index)) continue;
            var text = getText(index)?.Trim();
            if (!string.IsNullOrEmpty(text) && text.StartsWith(search, StringComparison.CurrentCultureIgnoreCase))
                return index;
        }
        return -1;
    }

    /// <summary>Appends <paramref name="key"/> to the search string and returns the best match, or null.</summary>
    public TItem? Search<TItem>(
        IReadOnlyList<TItem> items,
        string key,
        TItem? current,
        Func<TItem, string?> getText,
        Func<TItem, bool> isDisabled) where TItem : class
    {
        var currentIndex = -1;
        if (current is not null)
        {
            for (var i = 0; i < items.Count; i++)
                if (ReferenceEquals(items[i], current)) { currentIndex = i; break; }
        }
        var index = SearchIndex(items.Count, key, currentIndex, i => getText(items[i]), i => isDisabled(items[i]));
        return index >= 0 ? items[index] : null;
    }

    /// <summary>Convenience overload for <see cref="BitBhCollection{TItem}"/> members.</summary>
    public TItem? Search<TItem>(BitBhCollection<TItem> collection, string key, TItem? current)
        where TItem : class, IBitBhCollectionItem
        => Search(collection.Items, key, current, i => i.ItemText, i => i.IsItemDisabled);
}
