namespace Bit.Balette;

/// <summary>
/// All user-visible strings, the ones only screen readers hear included. Set properties, or subclass and override them
/// (for example to read them from an <c>IStringLocalizer</c>), to localize. Templates such as <see cref="NoResults"/>
/// are composite format strings: <c>{0}</c> is replaced, formatted with the current culture.
/// </summary>
public class BaletteStrings
{
    /// <summary>The search box placeholder on the root page.</summary>
    public virtual string Placeholder { get; set; } = "Type a command or search…";

    /// <summary>The search box placeholder of a prompt that set none of its own.</summary>
    public virtual string PromptPlaceholder { get; set; } = "Type a value and press Enter…";

    /// <summary>The empty state of a query that found nothing; <c>{0}</c> is the query.</summary>
    public virtual string NoResults { get; set; } = "No results for “{0}”";

    /// <summary>The hint under <see cref="NoResults"/>.</summary>
    public virtual string NoResultsHint { get; set; } = "Try a different search, or check for typos.";

    /// <summary>The empty state of a page with nothing on it.</summary>
    public virtual string Empty { get; set; } = "Nothing to show yet.";

    /// <summary>The loading state, shown and announced while a lazily loaded page or an async source has not answered.</summary>
    public virtual string Loading { get; set; } = "Searching…";

    /// <summary>The heading of the recently used commands.</summary>
    public virtual string Recent { get; set; } = "Recent";

    /// <summary>The heading of the pinned commands, and what a pinned row says to screen readers.</summary>
    public virtual string Pinned { get; set; } = "Pinned";

    /// <summary>The heading of the ungrouped commands when pinned or recent ones are listed above them.</summary>
    public virtual string Suggestions { get; set; } = "Suggestions";

    /// <summary>The heading of the mode hints on the empty query.</summary>
    public virtual string Modes { get; set; } = "Modes";

    /// <summary>The search box placeholder of the action panel.</summary>
    public virtual string Actions { get; set; } = "Actions";

    /// <summary>The accessible name of the result list on the root page.</summary>
    public virtual string Results { get; set; } = "Results";

    /// <summary>Not used by the palette itself; kept for templates.</summary>
    public virtual string Other { get; set; } = "Other";

    /// <summary>Footer hint after the ↑ ↓ keys.</summary>
    public virtual string Navigate { get; set; } = "to navigate";

    /// <summary>Footer hint after the Enter key.</summary>
    public virtual string Select { get; set; } = "to select";

    /// <summary>Footer hint after the Esc key on the root page.</summary>
    public virtual string Close { get; set; } = "to close";

    /// <summary>Footer hint after the Esc key in an inline palette with a query typed.</summary>
    public virtual string ClearQuery { get; set; } = "to clear";

    /// <summary>Footer hint after the Esc key on a nested page and in a prompt.</summary>
    public virtual string Back { get; set; } = "to go back";

    /// <summary>Footer hint after the Tab key.</summary>
    public virtual string OpenActions { get; set; } = "for actions";

    /// <summary>Footer hint after the Enter key on a multi-select page.</summary>
    public virtual string Toggle { get; set; } = "to toggle";

    /// <summary>Footer hint after Ctrl+Enter on a multi-select page, and after Enter in a prompt.</summary>
    public virtual string Apply { get; set; } = "to apply";

    /// <summary>The action panel entry that pins a command.</summary>
    public virtual string Pin { get; set; } = "Pin to top";

    /// <summary>The action panel entry that unpins a command.</summary>
    public virtual string Unpin { get; set; } = "Unpin";

    /// <summary>The action panel entry that forgets a command's uses.</summary>
    public virtual string RemoveFromRecents { get; set; } = "Remove from recents";

    /// <summary>The action panel entry that forgets every recent command (pins stay).</summary>
    public virtual string ClearRecents { get; set; } = "Clear recent commands";

    /// <summary>Put before the title of a command waiting for its confirming Enter.</summary>
    public virtual string ConfirmPrefix { get; set; } = "Confirm: ";

    /// <summary>Announced with the confirmation message of a command waiting for its second Enter.</summary>
    public virtual string ConfirmHint { get; set; } = "Press Enter again to confirm, Esc to cancel";

    /// <summary>How many rows are checked on a multi-select page; <c>{0}</c> is the count.</summary>
    public virtual string SelectedCount { get; set; } = "{0} selected";

    /// <summary>Announced after a search with several results; <c>{0}</c> is the count. See <see cref="ResultsAnnouncementFor"/>.</summary>
    public virtual string ResultsAnnouncement { get; set; } = "{0} results available";

    /// <summary>Announced after a search with exactly one result.</summary>
    public virtual string OneResultAnnouncement { get; set; } = "1 result available";

    /// <summary>Announced after a search with no result.</summary>
    public virtual string NoResultsAnnouncement { get; set; } = "No results";

    /// <summary>Announced when a page opens or the palette goes back to one; <c>{0}</c> is its title, <c>{1}</c> what it lists.</summary>
    public virtual string PageAnnouncement { get; set; } = "{0}. {1}";

    /// <summary>Shown when a disabled command without a reason is chosen.</summary>
    public virtual string Disabled { get; set; } = "Unavailable";

    /// <summary>Not used by the palette itself; kept for templates.</summary>
    public virtual string ModeHintTitle { get; set; } = "{0} — {1}";

    /// <summary>The accessible name of the dialog.</summary>
    public virtual string DialogLabel { get; set; } = "Command palette";

    /// <summary>The accessible name of the search box.</summary>
    public virtual string InputLabel { get; set; } = "Search commands";

    /// <summary>The accessible name of the close button.</summary>
    public virtual string CloseButton { get; set; } = "Close";

    /// <summary>The accessible name of the back button.</summary>
    public virtual string BackButton { get; set; } = "Back";

    /// <summary>The accessible name of the breadcrumb navigation.</summary>
    public virtual string Breadcrumb { get; set; } = "Breadcrumb";

    /// <summary>The accessible name of <see cref="BaletteTrigger"/>.</summary>
    public virtual string OpenPalette { get; set; } = "Open command palette";

    /// <summary>The label of <see cref="BaletteTrigger"/> when it sets none of its own.</summary>
    public virtual string Search { get; set; } = "Search";

    /// <summary>The title of the root page.</summary>
    public virtual string Home { get; set; } = "Home";

    /// <summary>The row that loads a source's next page when it said how many results there are; <c>{0}</c> is how many are left.</summary>
    public virtual string MoreResults { get; set; } = "Show {0} more…";

    /// <summary>The row that loads a source's next page when it did not say how many results there are.</summary>
    public virtual string LoadMore { get; set; } = "Show more…";

    /// <summary>Shown, and announced, after the first chords of a shortcut sequence (<c>G</c> of <c>G S</c>), until it completes or times out.</summary>
    public virtual string SequencePending { get; set; } = "waiting for the next key…";

    /// <summary>Shown when a command failed; <c>{0}</c> is its title. See <see cref="BaletteCommandErrorEventArgs.Message"/>.</summary>
    public virtual string CommandFailed { get; set; } = "Couldn’t run “{0}”.";

    /// <summary>Shown when the children of a page failed to load.</summary>
    public virtual string LoadFailed { get; set; } = "Couldn’t load this list.";

    /// <summary>The button and hint that load a failed list again.</summary>
    public virtual string Retry { get; set; } = "Try again";

    /// <summary>Shown above results found only by allowing typos (see <see cref="BaletteOptions.TypoTolerance"/>).</summary>
    public virtual string ApproximateResults { get; set; } = "No exact matches. Showing close ones.";

    /// <summary>The announcement of such results: <c>{0}</c> is <see cref="ApproximateResults"/>, <c>{1}</c> the result count.</summary>
    public virtual string ApproximateResultsAnnouncement { get; set; } = "{0} {1}";

    /// <summary>Announced, and shown by a spinner, while a command's handler has been running for a moment.</summary>
    public virtual string Running { get; set; } = "Working…";

    /// <summary>The row an async source that failed or timed out leaves; <c>{0}</c> is its name. Choosing it retries.</summary>
    public virtual string SourceFailed { get; set; } = "Couldn’t search {0}";

    /// <summary>What screen readers hear for a toggle that is on.</summary>
    public virtual string On { get; set; } = "On";

    /// <summary>What screen readers hear for a toggle that is off.</summary>
    public virtual string Off { get; set; } = "Off";

    /// <summary>The word between the chords of a sequence: "G then S".</summary>
    public virtual string ShortcutThen { get; set; } = "then";

    /// <summary>
    /// The announcement after a search that found <paramref name="count"/> results. Override it for languages whose
    /// plural forms one template per count cannot express; by default it picks <see cref="NoResultsAnnouncement"/>,
    /// <see cref="OneResultAnnouncement"/> or <see cref="ResultsAnnouncement"/>.
    /// </summary>
    public virtual string ResultsAnnouncementFor(int count) => count switch
    {
        0 => NoResultsAnnouncement,
        1 => OneResultAnnouncement,
        _ => Format(ResultsAnnouncement, count),
    };

    /// <summary>Format a template string with <see cref="string.Format(IFormatProvider, string, object[])"/>.</summary>
    public string Format(string template, params object[] args) => string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args);
}
