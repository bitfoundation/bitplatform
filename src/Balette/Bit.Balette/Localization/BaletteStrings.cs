namespace Bit.Balette;

/// <summary>All user-visible strings. Subclass and override, or set properties, to localize.</summary>
public class BaletteStrings
{
    public virtual string Placeholder { get; set; } = "Type a command or search…";
    public virtual string PromptPlaceholder { get; set; } = "Type a value and press Enter…";
    public virtual string NoResults { get; set; } = "No results for “{0}”";
    public virtual string NoResultsHint { get; set; } = "Try a different search, or check for typos.";
    public virtual string Empty { get; set; } = "Nothing to show yet.";
    public virtual string Loading { get; set; } = "Searching…";
    public virtual string Recent { get; set; } = "Recent";
    public virtual string Pinned { get; set; } = "Pinned";
    public virtual string Suggestions { get; set; } = "Suggestions";
    public virtual string Modes { get; set; } = "Modes";
    public virtual string Actions { get; set; } = "Actions";
    public virtual string Results { get; set; } = "Results";
    public virtual string Other { get; set; } = "Other";
    public virtual string Navigate { get; set; } = "to navigate";
    public virtual string Select { get; set; } = "to select";
    public virtual string Close { get; set; } = "to close";
    public virtual string Back { get; set; } = "to go back";
    public virtual string OpenActions { get; set; } = "for actions";
    public virtual string Toggle { get; set; } = "to toggle";
    public virtual string Apply { get; set; } = "to apply";
    public virtual string Pin { get; set; } = "Pin to top";
    public virtual string Unpin { get; set; } = "Unpin";
    public virtual string RemoveFromRecents { get; set; } = "Remove from recents";
    public virtual string ClearRecents { get; set; } = "Clear recent commands";
    public virtual string ConfirmPrefix { get; set; } = "Confirm: ";
    public virtual string ConfirmHint { get; set; } = "Press Enter again to confirm, Esc to cancel";
    public virtual string SelectedCount { get; set; } = "{0} selected";
    public virtual string ResultsAnnouncement { get; set; } = "{0} results available";
    public virtual string OneResultAnnouncement { get; set; } = "1 result available";
    public virtual string NoResultsAnnouncement { get; set; } = "No results";
    public virtual string Disabled { get; set; } = "Unavailable";
    public virtual string ModeHintTitle { get; set; } = "{0} — {1}";
    public virtual string DialogLabel { get; set; } = "Command palette";
    public virtual string InputLabel { get; set; } = "Search commands";
    public virtual string CloseButton { get; set; } = "Close";
    public virtual string BackButton { get; set; } = "Back";
    public virtual string OpenPalette { get; set; } = "Open command palette";
    public virtual string Search { get; set; } = "Search";
    public virtual string Home { get; set; } = "Home";
    public virtual string MoreResults { get; set; } = "Show {0} more…";

    /// <summary>Format a template string with <see cref="string.Format(IFormatProvider, string, object[])"/>.</summary>
    public string Format(string template, params object[] args) => string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args);
}
