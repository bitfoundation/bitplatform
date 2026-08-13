namespace Bit.Butil;

/// <summary>
/// One entry in the browser's session history, as reported by the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigationHistoryEntry">Navigation API</see>.
/// </summary>
/// <param name="Key">
/// Identifies the <i>slot</i> in the history list. It survives navigating away and coming back, so
/// this is what <see cref="Navigation.TraverseTo"/> takes and what you store to jump back to a
/// particular step later.
/// </param>
/// <param name="Id">
/// Identifies this <i>visit</i>. Unlike <paramref name="Key"/> it changes every time the entry is
/// re-visited, which makes it the right key for caching per-visit data.
/// </param>
/// <param name="Url">The entry's absolute URL.</param>
/// <param name="Index">
/// Its position in <see cref="Navigation.GetEntries"/>, or -1 for an entry that is not in the
/// current list.
/// </param>
/// <param name="SameDocument">
/// True when this entry belongs to the same document as the current one - i.e. traversing to it is
/// a client-side change rather than a page load.
/// </param>
public record NavigationEntry(
    string Key,
    string Id,
    string Url,
    int Index,
    bool SameDocument);

/// <summary>How a <see cref="Navigation.Navigate"/> call should affect the history list.</summary>
public enum NavigationHistoryBehavior
{
    /// <summary>Let the browser decide: push, unless the URL is unchanged, in which case replace.</summary>
    Auto,

    /// <summary>Always add a new entry.</summary>
    Push,

    /// <summary>Always overwrite the current entry, adding nothing to the back stack.</summary>
    Replace,
}

/// <summary>What happened, delivered to a <see cref="Navigation"/> subscription.</summary>
/// <param name="From">
/// For <see cref="Navigation.SubscribeCurrentEntryChange"/>, the entry that was current before the
/// change; null for the other events and for a change with no previous entry.
/// </param>
/// <param name="NavigationType">
/// <c>push</c>, <c>replace</c>, <c>reload</c> or <c>traverse</c> - null when the change was an
/// in-place state update rather than a navigation.
/// </param>
/// <param name="Message">The failure message, on <see cref="Navigation.SubscribeNavigateError"/> only.</param>
public record NavigationEventInfo(
    NavigationEntry? From,
    string? NavigationType,
    string? Message);
