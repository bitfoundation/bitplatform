namespace Bit.BlazorUI;

/// <summary>
/// The state of the suggest items of a <see cref="BitSearchBox"/> at the moment its screen reader
/// announcement is built, passed to the <see cref="BitSearchBox.AnnouncementProvider"/>.
/// </summary>
public class BitSearchBoxAnnouncementArgs(string? searchTerm,
                                          IReadOnlyList<string> suggestItems,
                                          bool isLoading,
                                          bool isSearchTermTooShort,
                                          int minSuggestTriggerChars)
{
    /// <summary>
    /// The current value of the search box that the suggest items were resolved for.
    /// </summary>
    public string? SearchTerm { get; } = searchTerm;

    /// <summary>
    /// The suggest items that are about to be rendered in the callout.
    /// </summary>
    public IReadOnlyList<string> SuggestItems { get; } = suggestItems;

    /// <summary>
    /// Whether an asynchronous <see cref="BitSearchBoxSuggestItemsProvider"/> is still resolving the suggest items.
    /// </summary>
    public bool IsLoading { get; } = isLoading;

    /// <summary>
    /// Whether the search term is still shorter than the <see cref="BitSearchBox.MinSuggestTriggerChars"/>,
    /// so no search was performed at all.
    /// </summary>
    public bool IsSearchTermTooShort { get; } = isSearchTermTooShort;

    /// <summary>
    /// The value of the <see cref="BitSearchBox.MinSuggestTriggerChars"/> parameter.
    /// </summary>
    public int MinSuggestTriggerChars { get; } = minSuggestTriggerChars;
}
