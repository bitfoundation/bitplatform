using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// The text the components write themselves - accessible names of parts that have no visible text ("Increase",
/// "Remove react"), and announcements ("3 results", "react removed"). English by default; change it for every
/// component at once:
/// <code>
/// builder.Services.AddBitBheadlessServices(strings =&gt;
/// {
///     strings.Increase = "Erhöhen";
///     strings.Decrease = "Verringern";
///     strings.RemoveTag = tag =&gt; $"{tag} entfernen";
/// });
/// </code>
/// For text that follows the request's culture, register your own instance per scope (built from an
/// <c>IStringLocalizer</c>, say): <c>services.AddScoped(sp =&gt; new BitBhStrings { … })</c>. A component's own
/// parameter (an <c>aria-label</c>, a <c>Label</c>, a message callback) always wins over these.
/// </summary>
public class BitBhStrings
{
    /// <summary>The instance used when none is registered.</summary>
    public static BitBhStrings Default { get; } = new();

    /// <summary>The button that steps a number field up.</summary>
    public string Increase { get; set; } = "Increase";

    /// <summary>The button that steps a number field down.</summary>
    public string Decrease { get; set; } = "Decrease";

    /// <summary>A position of a PIN input, from its index (0-based) and the count: "Character 2 of 6".</summary>
    public Func<int, int, string> PinInputSlot { get; set; } =
        (index, count) => string.Create(CultureInfo.CurrentCulture, $"Character {index + 1} of {count}");

    /// <summary>The list of tags of a tags input without a label of its own.</summary>
    public string Tags { get; set; } = "Tags";

    /// <summary>The remove button of a tag: "Remove react".</summary>
    public Func<string, string> RemoveTag { get; set; } = tag => $"Remove {tag}";

    /// <summary>Announced when a tag is removed: "react removed".</summary>
    public Func<string, string> TagRemoved { get; set; } = tag => $"{tag} removed";

    /// <summary>The button that removes every tag.</summary>
    public string ClearTags { get; set; } = "Clear all tags";

    /// <summary>The button that opens a combobox's options.</summary>
    public string ShowOptions { get; set; } = "Show options";

    /// <summary>The button that clears a combobox.</summary>
    public string ClearValue { get; set; } = "Clear";

    /// <summary>Announced when a command palette's results change: "3 results".</summary>
    public Func<int, string> Results { get; set; } =
        count => count == 1 ? "1 result" : string.Create(CultureInfo.CurrentCulture, $"{count} results");

    /// <summary>Announced when the typed text changes a combobox's options: "3 options available".</summary>
    public Func<int, string> OptionsAvailable { get; set; } =
        count => count == 1 ? "1 option available" : string.Create(CultureInfo.CurrentCulture, $"{count} options available");

    /// <summary>The value of an angle slider: "90 degrees".</summary>
    public Func<double, string> Angle { get; set; } =
        angle => angle == 1 ? "1 degree" : string.Create(CultureInfo.CurrentCulture, $"{angle} degrees");

    /// <summary>The command palette dialog.</summary>
    public string CommandPalette { get; set; } = "Command palette";

    /// <summary>The toast region.</summary>
    public string Notifications { get; set; } = "Notifications";

    /// <summary>The pagination landmark.</summary>
    public string Pagination { get; set; } = "Pagination";

    /// <summary>A page link of a pagination: "Page 4".</summary>
    public Func<int, string> Page { get; set; } = page => string.Create(CultureInfo.CurrentCulture, $"Page {page}");

    /// <summary>The previous-page button.</summary>
    public string PreviousPage { get; set; } = "Previous page";

    /// <summary>The next-page button.</summary>
    public string NextPage { get; set; } = "Next page";

    /// <summary>The first-page button.</summary>
    public string FirstPage { get; set; } = "First page";

    /// <summary>The last-page button.</summary>
    public string LastPage { get; set; } = "Last page";

    /// <summary>The breadcrumbs landmark.</summary>
    public string Breadcrumbs { get; set; } = "Breadcrumbs";

    /// <summary>The button that shows a password.</summary>
    public string ShowPassword { get; set; } = "Show password";

    /// <summary>The button that hides a password.</summary>
    public string HidePassword { get; set; } = "Hide password";

    /// <summary>The button that copies to the clipboard.</summary>
    public string Copy { get; set; } = "Copy";

    /// <summary>Announced when a copy succeeded.</summary>
    public string Copied { get; set; } = "Copied";

    /// <summary>The button that clears a search field.</summary>
    public string ClearSearch { get; set; } = "Clear search";

    /// <summary>A rating's star, from its value and the maximum: "3 stars".</summary>
    public Func<double, int, string> RatingItem { get; set; } =
        (value, max) => value == 1 ? "1 star" : string.Create(CultureInfo.CurrentCulture, $"{value} stars");

    /// <summary>A carousel's slide, from its index (0-based) and the count: "Slide 2 of 5".</summary>
    public Func<int, int, string> CarouselSlide { get; set; } =
        (index, count) => string.Create(CultureInfo.CurrentCulture, $"{index + 1} of {count}");

    /// <summary>The button that shows the previous slide.</summary>
    public string PreviousSlide { get; set; } = "Previous slide";

    /// <summary>The button that shows the next slide.</summary>
    public string NextSlide { get; set; } = "Next slide";

    /// <summary>A carousel indicator, from its index (0-based): "Go to slide 2".</summary>
    public Func<int, string> GoToSlide { get; set; } = index => string.Create(CultureInfo.CurrentCulture, $"Go to slide {index + 1}");

    /// <summary>The button that starts an autoplaying carousel.</summary>
    public string StartAutoplay { get; set; } = "Start automatic slide show";

    /// <summary>The button that stops an autoplaying carousel.</summary>
    public string StopAutoplay { get; set; } = "Stop automatic slide show";

    /// <summary>The button that removes a file from a file upload: "Remove report.pdf".</summary>
    public Func<string, string> RemoveFile { get; set; } = name => $"Remove {name}";

    /// <summary>The handle that resizes split panels.</summary>
    public string ResizePanels { get; set; } = "Resize panels";

    /// <summary>The handle of a drawer with snap points - the slider that sets how far the drawer is open.</summary>
    public string DrawerSize { get; set; } = "Drawer size";

    /// <summary>The snap point a drawer rests at, from its index (0-based, smallest first) and the count: "2 of 3".</summary>
    public Func<int, int, string> DrawerSnapPoint { get; set; } =
        (index, count) => string.Create(CultureInfo.CurrentCulture, $"{index + 1} of {count}");

    /// <summary>
    /// The instructions a sortable list's drag handles are described by when the list exchanges items with the other
    /// lists of its <c>Group</c>, from the list's orientation (which arrow keys move between the lists).
    /// </summary>
    public Func<BitBhOrientation, string> SortableGroupInstructions { get; set; } = orientation => orientation == BitBhOrientation.Vertical
        ? "To pick up an item, press Space or Enter. While dragging, use the up and down arrow keys to move it within the list and the left and right arrow keys to move it to another list. Press Space or Enter again to drop it, or Escape to cancel."
        : "To pick up an item, press Space or Enter. While dragging, use the left and right arrow keys to move it within the list and the up and down arrow keys to move it to another list. Press Space or Enter again to drop it, or Escape to cancel.";

    /// <summary>A sortable list without a label of its own, from its position (1-based) in its group: "list 2".</summary>
    public Func<int, string> SortableList { get; set; } = position => string.Create(CultureInfo.CurrentCulture, $"list {position}");

    /// <summary>
    /// Announced when a picked-up item moves to another list, from the item's label, the list's name, the position
    /// (1-based) and the list's count: "Fix login moved to Done, position 1 of 3.".
    /// </summary>
    public Func<string, string, int, int, string> SortableMovedToList { get; set; } =
        (label, list, position, count) => string.Create(CultureInfo.CurrentCulture, $"{label} moved to {list}, position {position} of {count}.");

    /// <summary>
    /// Announced when an item is dropped in another list, from the item's label, the list's name, the position
    /// (1-based) and the list's count: "Dropped Fix login in Done at position 1 of 3.".
    /// </summary>
    public Func<string, string, int, int, string> SortableDroppedInList { get; set; } =
        (label, list, position, count) => string.Create(CultureInfo.CurrentCulture, $"Dropped {label} in {list} at position {position} of {count}.");

    /// <summary>
    /// Announced when a move that had taken an item to another list is cancelled, from the item's label, the name of
    /// the list it came from and its position (1-based) there: "Moving cancelled. Fix login returned to To do, position 2.".
    /// </summary>
    public Func<string, string, int, string> SortableReturnedToList { get; set; } =
        (label, list, position) => string.Create(CultureInfo.CurrentCulture, $"Moving cancelled. {label} returned to {list}, position {position}.");

    /// <summary>The button that opens a date picker's calendar.</summary>
    public string ChooseDate { get; set; } = "Choose date";

    /// <summary>The button that opens a date range picker's calendar.</summary>
    public string ChooseDateRange { get; set; } = "Choose date range";

    /// <summary>The segments of the first date of a date range picker.</summary>
    public string StartDate { get; set; } = "Start date";

    /// <summary>The segments of the last date of a date range picker.</summary>
    public string EndDate { get; set; } = "End date";

    /// <summary>The button that shows the previous month of a calendar.</summary>
    public string PreviousMonth { get; set; } = "Previous month";

    /// <summary>The button that shows the next month of a calendar.</summary>
    public string NextMonth { get; set; } = "Next month";

    /// <summary>Announced when a calendar's date is selected, from the formatted date: "Selected date: Sunday, November 8, 2026".</summary>
    public Func<string, string> SelectedDate { get; set; } = date => $"Selected date: {date}";

    /// <summary>Announced when a calendar's range is selected, from the formatted start and end: "Selected range: … to …".</summary>
    public Func<string, string, string> SelectedRange { get; set; } = (start, end) => $"Selected range: {start} to {end}";

    /// <summary>
    /// The accessible name of a date / time field segment: "year", "month", "day", "hour", "minute", "second", "AM/PM".
    /// </summary>
    public Func<BitBhDateSegmentType, string> DateSegmentName { get; set; } = type => type switch
    {
        BitBhDateSegmentType.Year => "year",
        BitBhDateSegmentType.Month => "month",
        BitBhDateSegmentType.Day => "day",
        BitBhDateSegmentType.Hour => "hour",
        BitBhDateSegmentType.Minute => "minute",
        BitBhDateSegmentType.Second => "second",
        BitBhDateSegmentType.DayPeriod => "AM/PM",
        _ => string.Empty,
    };

    /// <summary>
    /// The text an empty date / time field segment shows: "yyyy", "mm", "dd", and two en dashes for the time segments
    /// (an empty AM/PM segment shows the culture's AM designator).
    /// </summary>
    public Func<BitBhDateSegmentType, string> DateSegmentPlaceholder { get; set; } = type => type switch
    {
        BitBhDateSegmentType.Year => "yyyy",
        BitBhDateSegmentType.Month => "mm",
        BitBhDateSegmentType.Day => "dd",
        _ => new string((char)0x2013, 2),
    };

    /// <summary>The value announced for an empty date / time field segment (<c>aria-valuetext</c>).</summary>
    public string EmptySegment { get; set; } = "Empty";

    /// <summary>The landmark of a table of contents.</summary>
    public string TableOfContents { get; set; } = "On this page";

    /// <summary>The steps of a stepper.</summary>
    public string Steps { get; set; } = "Steps";
}
