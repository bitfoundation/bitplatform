using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Pagination component helps users easily navigate through content, allowing swift browsing across multiple pages or sections, commonly used in lists, tables, and content-rich interfaces.
/// </summary>
/// <remarks>
/// The component renders a navigation landmark holding a list of page controls, marks the current page with
/// aria-current and names every control for assistive technologies, so it can be dropped next to a grid or a
/// list of results without any extra markup.
/// <br />
/// Name the landmark through <see cref="BitComponentBase.AriaLabel"/> whenever more than one pagination is
/// rendered on the same page, so each of them can be told apart in the landmark list.
/// <br />
/// The controls are buttons by default and become links as soon as <see cref="GetPageHref"/> hands them an
/// address, which is what a range that is meant to be crawled, bookmarked or opened in another tab calls for.
/// <br />
/// The range of pages is either given as a <see cref="Count"/> or worked out from <see cref="TotalItems"/> and
/// <see cref="PageSize"/>. The second is the form to reach for whenever the size of the result set is known,
/// since the range then follows the page size on its own.
/// </remarks>
public partial class BitPagination : BitComponentBase
{
    /// <summary>
    /// The placeholder a generated page list uses in place of the pages that are collapsed into an ellipsis.
    /// </summary>
    private const int EllipsisPage = -1;

    private const int DefaultMiddleCount = 3;
    private const int DefaultBoundaryCount = 2;

    private static readonly int[] DefaultPageSizeOptions = [10, 25, 50, 100];

    /// <summary>
    /// The control the focus is handed over to after a navigation button disabled itself by moving the
    /// selection to the end of the range it points at.
    /// </summary>
    private enum FocusTarget { None, SelectedPage, First, Previous, Next, Last }

    private int _correctedPage;
    private string? _goToPageText;
    private bool _correctedPageSize;

    // The pages the last render put on the screen, which is what the captured element references are pruned
    // against so that walking a long range does not keep a reference of every page it went through.
    private int[] _renderedPages = [];

    // The offered page sizes are materialized once per parameter change rather than on every render, since a
    // consumer is free to hand over an enumerable that walks (or computes) itself each time it is read.
    private int[] _pageSizeOptions = DefaultPageSizeOptions;

    private FocusTarget _focusTarget;
    private ElementReference _firstButtonRef;
    private ElementReference _previousButtonRef;
    private ElementReference _nextButtonRef;
    private ElementReference _lastButtonRef;

    // The page buttons are captured by their page number so that the one holding the selection can be found
    // again after the range around it moved. Entries of the pages that left the range are harmless: the focus
    // is only ever handed to the page the pagination just settled on, which is always one of the rendered ones.
    private readonly Dictionary<int, ElementReference> _pageRefs = [];



    /// <summary>
    /// The horizontal alignment of the pagination inside the room it is given.
    /// </summary>
    /// <remarks>
    /// The pagination is only as wide as its controls by default, so it sits wherever the layout around it puts
    /// it. Setting an alignment stretches it across the room it is given and lines the controls up inside that
    /// room, which is what saves wrapping it in a flex container of its own.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// The number of items at the start and end of the pagination.
    /// <br />
    /// The default value is <strong>2</strong>.
    /// </summary>
    /// <remarks>
    /// A value that is not positive falls back to the default, since a range with no fixed ends would lose
    /// the shortcut to the first and the last pages.
    /// </remarks>
    [Parameter] public int BoundaryCount { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the pagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Classes { get; set; }

    /// <summary>
    /// Turns every ellipsis into a control that jumps into the middle of the pages it collapses.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A long range is otherwise walked one window at a time, so the gap becoming a shortcut into itself is what
    /// makes the middle of a few hundred pages reachable in a couple of clicks.
    /// <br />
    /// The ellipsis follows the rest of the pagination: it is a button, or a link as soon as
    /// <see cref="GetPageHref"/> hands it an address, and it takes its accessible name from
    /// <see cref="EllipsisAriaLabel"/>. While it is off, the ellipsis stays the plain text it is by default and
    /// is hidden from assistive technologies.
    /// </remarks>
    [Parameter] public bool ClickableEllipsis { get; set; }

    /// <summary>
    /// The general color of the pagination.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The total number of pages.
    /// <br />
    /// The default value is <strong>1</strong>.
    /// </summary>
    /// <remarks>
    /// A count that is not positive still leaves a single page to be on, since a pagination with no page at
    /// all has nothing to render.
    /// <br />
    /// It is ignored while <see cref="TotalItems"/> is set, since the number of pages then follows from the
    /// number of items and the page size.
    /// </remarks>
    [Parameter] public int Count { get; set; }

    /// <summary>
    /// The default selected page number.
    /// </summary>
    [Parameter] public int DefaultSelectedPage { get; set; }

    /// <summary>
    /// The accessible label of the item standing in for the pages an ellipsis collapses.
    /// <br />
    /// The default value is <strong>"More pages"</strong>.
    /// </summary>
    /// <remarks>
    /// A plain gap keeps its glyph hidden from assistive technologies and lets the item around it carry this
    /// label, so it is reported as one item instead of being read as a run of punctuation. A
    /// <see cref="ClickableEllipsis"/> is a control of its own, so it is named by this label directly and the
    /// item around it is left unnamed beside it.
    /// </remarks>
    [Parameter] public string EllipsisAriaLabel { get; set; } = "More pages";

    /// <summary>
    /// The text of the ellipsis standing in for the pages that are collapsed out of the range.
    /// <br />
    /// The default value is <strong>"•••"</strong>.
    /// </summary>
    [Parameter] public string EllipsisText { get; set; } = "•••";

    /// <summary>
    /// The accessible label of the first button.
    /// <br />
    /// The default value is <strong>"First page"</strong>.
    /// </summary>
    /// <remarks>
    /// The value is used both as the aria-label and as the native tooltip of the button, since the button
    /// carries an icon and no text of its own. The tooltip is dropped as soon as
    /// <see cref="FirstButtonText"/> puts a text beside that icon, where it would only repeat what is already
    /// on screen, while the spoken name stays this one.
    /// </remarks>
    [Parameter] public string FirstButtonAriaLabel { get; set; } = "First page";

    /// <summary>
    /// The icon for the first button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="FirstButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? FirstButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the first button.
    /// For external icon libraries, use <see cref="FirstButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? FirstButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the first button.
    /// </summary>
    /// <remarks>
    /// A navigation button carries an icon only unless it is given a text, and it widens to fit the text it
    /// is given. The accessible name still comes from <see cref="FirstButtonAriaLabel"/>, so a short visible
    /// text can sit next to a fuller spoken one.
    /// </remarks>
    [Parameter] public string? FirstButtonText { get; set; }

    /// <summary>
    /// Provides the accessible label of a page button, from its one-based number and whether it is the
    /// selected one, replacing the default "Page {number}" label.
    /// </summary>
    /// <remarks>
    /// This is the hook to localize the page buttons, or to make them announce what the page holds
    /// (for example "Page 3 of 12" or "Results 21 to 30").
    /// <br />
    /// The selected page also reports aria-current, so the label does not have to say that it is the
    /// current one for a screen reader to announce it as such.
    /// </remarks>
    [Parameter] public Func<int, bool, string>? GetPageAriaLabel { get; set; }

    /// <summary>
    /// Provides the address a page control points at, from its one-based number, which turns every control of
    /// the pagination into a link instead of a button.
    /// </summary>
    /// <remarks>
    /// Pagination is navigation, so a range that is reachable by its own address (a crawler following it, a
    /// page opened in another tab, a middle click) belongs in links rather than in buttons. The four
    /// navigation controls ask for the address of the page they move to, so they turn into links along with
    /// the numeric ones.
    /// <br />
    /// A control with no address to point at - one at the end of the range it navigates, one the pagination is
    /// disabled for, or one this returns nothing for - keeps its place while reporting aria-disabled and
    /// staying out of the tab order.
    /// <br />
    /// The click still reaches <see cref="OnChange"/> and <see cref="SelectedPage"/>, so a pagination of links
    /// reports the page that was asked for exactly like a pagination of buttons.
    /// </remarks>
    [Parameter] public Func<int, string?>? GetPageHref { get; set; }

    /// <summary>
    /// Provides the text of the summary, from the selected page and the total number of pages, replacing the
    /// default "Page {number} of {count}" text.
    /// </summary>
    /// <remarks>
    /// This is the hook to localize the summary, or to report the position in terms of the items rather than
    /// the pages (for example "Showing 21 to 30 of 240 results") from numbers only the consumer holds.
    /// <br />
    /// The default text already counts the items ("1 - 10 of 240") whenever <see cref="TotalItems"/> is set, so
    /// this is only needed there to word or localize that text differently.
    /// <br />
    /// It is only called while <see cref="ShowSummary"/> is on.
    /// </remarks>
    [Parameter] public Func<int, int, string>? GetSummary { get; set; }

    /// <summary>
    /// The accessible label of the go to page input.
    /// <br />
    /// The default value is <strong>"Go to page"</strong>.
    /// </summary>
    /// <remarks>
    /// It steps in once the visible <see cref="GoToPageText"/> beside the input is dropped, so the input is
    /// named either way. While that text is there it is the one naming the input, since a label a control is
    /// announced with that differs from the one written beside it is a control speech input cannot reach.
    /// </remarks>
    [Parameter] public string GoToPageAriaLabel { get; set; } = "Go to page";

    /// <summary>
    /// The text rendered ahead of the go to page input.
    /// <br />
    /// The default value is <strong>"Go to"</strong>.
    /// </summary>
    /// <remarks>
    /// An empty text leaves the input on its own, which is the compact form a narrow layout calls for.
    /// </remarks>
    [Parameter] public string? GoToPageText { get; set; } = "Go to";

    /// <summary>
    /// Renders nothing at all while there is a single page to navigate.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Navigation that cannot go anywhere is noise, so hiding it keeps the layout of a short result set clean.
    /// Leave it off when the pagination sits in a fixed layout that a disappearing element would reflow.
    /// </remarks>
    [Parameter] public bool HideOnSinglePage { get; set; }

    /// <summary>
    /// The accessible label of the last button.
    /// <br />
    /// The default value is <strong>"Last page"</strong>.
    /// </summary>
    /// <remarks>
    /// The value is used both as the aria-label and as the native tooltip of the button, since the button
    /// carries an icon and no text of its own. The tooltip is dropped as soon as
    /// <see cref="LastButtonText"/> puts a text beside that icon, where it would only repeat what is already
    /// on screen, while the spoken name stays this one.
    /// </remarks>
    [Parameter] public string LastButtonAriaLabel { get; set; } = "Last page";

    /// <summary>
    /// The icon for the last button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="LastButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? LastButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the last button.
    /// For external icon libraries, use <see cref="LastButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? LastButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the last button.
    /// </summary>
    /// <remarks>
    /// A navigation button carries an icon only unless it is given a text, and it widens to fit the text it
    /// is given. The accessible name still comes from <see cref="LastButtonAriaLabel"/>, so a short visible
    /// text can sit next to a fuller spoken one.
    /// </remarks>
    [Parameter] public string? LastButtonText { get; set; }

    /// <summary>
    /// Wraps the next and previous buttons around the ends of the range, so the next button moves from the
    /// last page to the first one and the previous button from the first page to the last one.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The two buttons also stay enabled at the ends of the range while this is on. The first and last
    /// buttons are unaffected, since they always target a fixed page.
    /// </remarks>
    [Parameter] public bool Loop { get; set; }

    /// <summary>
    /// The number of items to render in the middle of the pagination.
    /// <br />
    /// The default value is <strong>3</strong>.
    /// </summary>
    /// <remarks>
    /// A value that is not positive falls back to the default, since a middle range with nothing in it would
    /// leave the selected page out of the pagination.
    /// </remarks>
    [Parameter] public int MiddleCount { get; set; }

    /// <summary>
    /// The accessible label of the next button.
    /// <br />
    /// The default value is <strong>"Next page"</strong>.
    /// </summary>
    /// <remarks>
    /// The value is used both as the aria-label and as the native tooltip of the button, since the button
    /// carries an icon and no text of its own. The tooltip is dropped as soon as
    /// <see cref="NextButtonText"/> puts a text beside that icon, where it would only repeat what is already
    /// on screen, while the spoken name stays this one.
    /// </remarks>
    [Parameter] public string NextButtonAriaLabel { get; set; } = "Next page";

    /// <summary>
    /// The icon for the next button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the next button.
    /// For external icon libraries, use <see cref="NextButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? NextButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the next button.
    /// </summary>
    /// <remarks>
    /// A navigation button carries an icon only unless it is given a text, and it widens to fit the text it
    /// is given. The accessible name still comes from <see cref="NextButtonAriaLabel"/>, so a short visible
    /// text can sit next to a fuller spoken one.
    /// </remarks>
    [Parameter] public string? NextButtonText { get; set; }

    /// <summary>
    /// The event callback for when selected page changes.
    /// </summary>
    /// <remarks>
    /// The callback also runs when <see cref="SelectedPage"/> is bound one way, so a page can be requested
    /// and applied by the consumer without giving up control of the value.
    /// <br />
    /// A page size that moves the selection (which only happens while <see cref="TotalItems"/> is set) reports
    /// the page it moved to through here as well, ahead of <see cref="OnPageSizeChange"/>.
    /// </remarks>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The event callback for when the page size is picked out of the page size selector.
    /// </summary>
    /// <remarks>
    /// The callback also runs when <see cref="PageSize"/> is bound one way, and it is where the consumer
    /// recomputes <see cref="Count"/> from the page size it was handed. While <see cref="TotalItems"/> is set
    /// there is nothing left to recompute, and this only reports the size that was picked.
    /// <br />
    /// It runs after <see cref="OnChange"/>, so a page the new size moved the selection to has already been
    /// reported by the time it is reached.
    /// </remarks>
    [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }

    /// <summary>
    /// The number of items a page holds, which the page size selector picks.
    /// </summary>
    /// <remarks>
    /// While <see cref="TotalItems"/> is set the range of pages follows the page size on its own. Otherwise the
    /// pagination only reports the number that was picked and the range still comes from <see cref="Count"/>,
    /// which the consumer recomputes from the new page size.
    /// <br />
    /// A value that is not positive falls back to the first of the <see cref="PageSizeOptions"/>, and the
    /// fallback is written back while the selector is shown, so the size the consumer holds is the one the
    /// selector opens on. A value that is positive but not among the offered ones is offered by the selector
    /// along with them rather than being left unreachable.
    /// </remarks>
    [Parameter, TwoWayBound]
    public int PageSize { get; set; }

    /// <summary>
    /// The accessible label of the page size selector.
    /// <br />
    /// The default value is <strong>"Items per page"</strong>.
    /// </summary>
    /// <remarks>
    /// It steps in once the visible <see cref="PageSizeText"/> beside the selector is dropped, so the selector
    /// is named either way. While that text is there it is the one naming the selector, since a label a control
    /// is announced with that differs from the one written beside it is a control speech input cannot reach.
    /// </remarks>
    [Parameter] public string PageSizeAriaLabel { get; set; } = "Items per page";

    /// <summary>
    /// The page sizes the page size selector offers.
    /// <br />
    /// The default value is <strong>10, 25, 50 and 100</strong>.
    /// </summary>
    /// <remarks>
    /// An empty list (or one holding nothing but sizes that are not positive, which are dropped) falls back
    /// to the default, since a selector with nothing to pick from would leave the page size it reports
    /// unreachable.
    /// <br />
    /// A <see cref="PageSize"/> the list does not hold is offered along with the others, so the selector
    /// always shows the size the pagination is paging by.
    /// </remarks>
    [Parameter] public IEnumerable<int>? PageSizeOptions { get; set; }

    /// <summary>
    /// The text rendered ahead of the page size selector.
    /// <br />
    /// The default value is <strong>"Items per page"</strong>.
    /// </summary>
    /// <remarks>
    /// An empty text leaves the selector on its own, which is the compact form a narrow layout calls for.
    /// </remarks>
    [Parameter] public string? PageSizeText { get; set; } = "Items per page";

    /// <summary>
    /// The accessible label of the previous button.
    /// <br />
    /// The default value is <strong>"Previous page"</strong>.
    /// </summary>
    /// <remarks>
    /// The value is used both as the aria-label and as the native tooltip of the button, since the button
    /// carries an icon and no text of its own. The tooltip is dropped as soon as
    /// <see cref="PreviousButtonText"/> puts a text beside that icon, where it would only repeat what is already
    /// on screen, while the spoken name stays this one.
    /// </remarks>
    [Parameter] public string PreviousButtonAriaLabel { get; set; } = "Previous page";

    /// <summary>
    /// The icon for the previous button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PreviousButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PreviousButtonIcon { get; set; }

    /// <summary>
    /// The built-in icon name for the previous button.
    /// For external icon libraries, use <see cref="PreviousButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? PreviousButtonIconName { get; set; }

    /// <summary>
    /// The text rendered beside the icon of the previous button.
    /// </summary>
    /// <remarks>
    /// A navigation button carries an icon only unless it is given a text, and it widens to fit the text it
    /// is given. The accessible name still comes from <see cref="PreviousButtonAriaLabel"/>, so a short
    /// visible text can sit next to a fuller spoken one.
    /// </remarks>
    [Parameter] public string? PreviousButtonText { get; set; }

    /// <summary>
    /// Renders the buttons of the pagination with fully rounded (circular) corners.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// The selected page number.
    /// </summary>
    /// <remarks>
    /// The value is one-based and is clamped into the available range while rendering, so a page number
    /// outside of <see cref="Count"/> never leaves the pagination without a current page.
    /// </remarks>
    [Parameter, TwoWayBound]
    public int SelectedPage { get; set; }

    /// <summary>
    /// Determines whether to show the first button.
    /// </summary>
    [Parameter] public bool ShowFirstButton { get; set; }

    /// <summary>
    /// Shows an input that jumps straight to the page number typed into it, at the end of the pagination.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The jump runs when the input is committed (on Enter, or when it loses the focus) and the input clears
    /// itself afterwards. A number outside of the range lands on the nearest end of it instead of being
    /// dropped, so a long range can be reached without knowing where it stops.
    /// </remarks>
    [Parameter] public bool ShowGoToPage { get; set; }

    /// <summary>
    /// Determines whether to show the last button.
    /// </summary>
    [Parameter] public bool ShowLastButton { get; set; }

    /// <summary>
    /// Determines whether to show the next button.
    /// </summary>
    [Parameter] public bool ShowNextButton { get; set; } = true;

    /// <summary>
    /// Determines whether to show the numeric page buttons.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// Turning the page buttons off leaves only the navigation buttons, which is the compact pagination a
    /// narrow layout or an unbounded result set (where the number of pages is unknown) calls for.
    /// </remarks>
    [Parameter] public bool ShowPageButtons { get; set; } = true;

    /// <summary>
    /// Shows a selector that picks how many items a page holds, ahead of everything else in the pagination.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Picking a size reports it through <see cref="PageSize"/> and <see cref="OnPageSizeChange"/>. While
    /// <see cref="TotalItems"/> is set the range of pages is recomputed from it and the selection is moved to
    /// the page the first item of the old one landed on. Otherwise the number of pages the new size adds up to
    /// is the consumer's to recompute into <see cref="Count"/>, and a selected page that falls out of the
    /// shrunk range is pulled back on its own.
    /// </remarks>
    [Parameter] public bool ShowPageSizeSelector { get; set; }

    /// <summary>
    /// Determines whether to show the previous button.
    /// </summary>
    [Parameter] public bool ShowPreviousButton { get; set; } = true;

    /// <summary>
    /// Shows the position in the range, which reads "Page {number} of {count}" (or "1 - 10 of 240" while
    /// <see cref="TotalItems"/> is set) unless <see cref="GetSummary"/> replaces it, ahead of the buttons of
    /// the pagination.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The summary is a status region, so a screen reader reports the new position as the page changes. That
    /// makes it the piece to turn on along with <see cref="ShowPageButtons"/> turned off, where nothing else
    /// tells which page of how many is the current one.
    /// </remarks>
    [Parameter] public bool ShowSummary { get; set; }

    /// <summary>
    /// The size of the buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the pagination.
    /// </summary>
    [Parameter] public BitPaginationClassStyles? Styles { get; set; }

    /// <summary>
    /// The total number of items the pagination pages through, which the number of pages is worked out from
    /// along with <see cref="PageSize"/>, replacing <see cref="Count"/>.
    /// </summary>
    /// <remarks>
    /// This is the form to reach for whenever the size of the result set is known: the range of pages follows
    /// the page size on its own, so the page size selector needs nothing wired up behind it, and the summary
    /// reports the items ("1 - 10 of 240") instead of the pages.
    /// <br />
    /// Picking a new page size keeps the first item of the current page in view rather than holding on to the
    /// page number, which is what stops a jump to the end of a shrinking range.
    /// <br />
    /// A value that is not positive leaves <see cref="Count"/> in charge, which is the form to keep when the
    /// number of items is unknown and only the number of pages is.
    /// </remarks>
    [Parameter] public int TotalItems { get; set; }

    /// <summary>
    /// The visual variant of the pagination.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Gives the keyboard focus to the button of the selected page, falling back to the first navigation
    /// button that is rendered while the page buttons are turned off.
    /// </summary>
    /// <remarks>
    /// This is what a consumer reloading the list behind the pagination calls to put the focus back on the
    /// navigation the reload was asked from.
    /// <br />
    /// It does nothing while the pagination renders no control at all, so a reload landing on a single page of
    /// results is not answered with a focus call on markup that is not there.
    /// </remarks>
    public ValueTask FocusAsync()
    {
        // A pagination that renders nothing holds no control the focus could go to, and the element references
        // left behind by an earlier render point at markup that has since been removed.
        if (_IsHidden) return ValueTask.CompletedTask;

        if (ShowPageButtons && _pageRefs.TryGetValue(_SelectedPage, out var pageRef)) return Focus(pageRef);

        if (ShowFirstButton) return Focus(_firstButtonRef);

        if (ShowPreviousButton) return Focus(_previousButtonRef);

        if (ShowNextButton) return Focus(_nextButtonRef);

        if (ShowLastButton) return Focus(_lastButtonRef);

        return ValueTask.CompletedTask;
    }

    // A control that was never rendered (the whole pagination hidden by HideOnSinglePage, or a first render
    // that has not happened yet) leaves an empty reference behind, which throws instead of doing nothing when
    // it is focused.
    private static ValueTask Focus(ElementReference element)
    {
        return element.Context is null ? ValueTask.CompletedTask : element.FocusAsync();
    }



    protected override string RootElementClass => "bit-pgn";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-pgn-pri",
            BitColor.Secondary => "bit-pgn-sec",
            BitColor.Tertiary => "bit-pgn-ter",
            BitColor.Info => "bit-pgn-inf",
            BitColor.Success => "bit-pgn-suc",
            BitColor.Warning => "bit-pgn-wrn",
            BitColor.SevereWarning => "bit-pgn-swr",
            BitColor.Error => "bit-pgn-err",
            _ => "bit-pgn-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-pgn-sm",
            BitSize.Medium => "bit-pgn-md",
            BitSize.Large => "bit-pgn-lg",
            _ => "bit-pgn-md"
        });

        ClassBuilder.Register(() => Rounded ? "bit-pgn-rnd" : string.Empty);

        // The alignment only means something once the pagination is stretched across the room it is given, so
        // the class that does the stretching is what the alignment itself is rendered through.
        ClassBuilder.Register(() => Alignment.HasValue ? "bit-pgn-aln" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Alignment switch
        {
            BitAlignment.Start => "--bit-pgn-justify-content:flex-start",
            BitAlignment.End => "--bit-pgn-justify-content:flex-end",
            BitAlignment.Center => "--bit-pgn-justify-content:center",
            BitAlignment.SpaceBetween => "--bit-pgn-justify-content:space-between",
            BitAlignment.SpaceAround => "--bit-pgn-justify-content:space-around",
            BitAlignment.SpaceEvenly => "--bit-pgn-justify-content:space-evenly",
            BitAlignment.Baseline => "--bit-pgn-justify-content:baseline",
            BitAlignment.Stretch => "--bit-pgn-justify-content:stretch",
            _ => string.Empty
        });

        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        // The offered page sizes are needed before the first parameter set is over, since the page size is what
        // the number of pages the default selection is clamped into follows from.
        UpdatePageSizeOptions();

        if (SelectedPageHasBeenSet is false && DefaultSelectedPage != 0)
        {
            await AssignSelectedPage(Math.Clamp(DefaultSelectedPage, 1, _Count));
        }

        if (SelectedPage == 0)
        {
            await AssignSelectedPage(1);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        UpdatePageSizeOptions();

        // A page size that the selector cannot report (a value that is not positive) is written back the same
        // way an out of range selected page is, so that the size the consumer holds is the one the selector
        // opens on and the paging math on both sides agrees. Only the rendered selector reports a size, so a
        // pagination without one leaves the value alone.
        if (PageSize == _PageSize)
        {
            _correctedPageSize = false;
        }
        else if (ShowPageSizeSelector && _correctedPageSize is false)
        {
            // The same fallback is only written once: a consumer that drops it would otherwise be answered
            // with another correction on every render, and the two would keep re-rendering each other.
            _correctedPageSize = true;

            await AssignPageSize(_PageSize);
        }

        // A selected page that fell outside of the range (a count that shrank under it, or a value that was
        // never inside it) is written back so that the value a consumer is bound to never keeps pointing at a
        // page that does not exist. This runs after every parameter has been applied, so a count and a
        // selected page changing together settle on the value the consumer asked for and not on an
        // intermediate one.
        if (SelectedPage == _SelectedPage)
        {
            _correctedPage = 0;
        }
        else if (SelectedPage != _correctedPage)
        {
            // The same out of range value is only corrected once: a consumer that hands it back unchanged
            // (a callback that drops the new page instead of storing it) would otherwise be answered with
            // another correction on every render, and the two would keep re-rendering each other.
            _correctedPage = SelectedPage;

            await AssignSelectedPage(_SelectedPage);
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        PruneStalePageReferences();

        // A navigation button that disabled itself by reaching the end of the range it points at drops the
        // keyboard focus on the document, so the focus is handed over to the control that took its place
        // (the selected page, or the navigation button pointing the other way) once the new markup is there.
        // A pagination that hid itself in the meantime has no control left to hand it to.
        if (_IsHidden)
        {
            _focusTarget = FocusTarget.None;
        }
        else if (_focusTarget != FocusTarget.None)
        {
            var target = _focusTarget;
            _focusTarget = FocusTarget.None;

            switch (target)
            {
                case FocusTarget.SelectedPage:
                    if (_pageRefs.TryGetValue(_SelectedPage, out var pageRef))
                    {
                        await Focus(pageRef);
                    }
                    break;
                case FocusTarget.First: await Focus(_firstButtonRef); break;
                case FocusTarget.Previous: await Focus(_previousButtonRef); break;
                case FocusTarget.Next: await Focus(_nextButtonRef); break;
                case FocusTarget.Last: await Focus(_lastButtonRef); break;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    // A known number of items settles the number of pages on its own, which is what keeps the range and the page
    // size in step without the consumer wiring the two together. There is always at least one page to be on, so
    // neither an item count nor a page count that is not positive renders an empty pagination.
    private int _Count => TotalItems > 0
        ? (int)Math.Min(int.MaxValue, ((long)TotalItems + _PageSize - 1) / _PageSize)
        : (Count > 0 ? Count : 1);

    // Nothing at all is rendered for a single page while the pagination is asked to stay out of the way, so the
    // markup the focus and the element references point at is gone along with it.
    private bool _IsHidden => HideOnSinglePage && _Count <= 1;

    // The rendering runs off a clamped view of the selected page so that a value outside of the range (which
    // a one way bound SelectedPage can hold, since the component cannot write it back) still renders a
    // pagination with a current page and with the right buttons disabled.
    private int _SelectedPage => Math.Clamp(SelectedPage, 1, _Count);

    // A page size that was never picked falls back to the first of the offered ones, so the selector opens on
    // a size that is actually one of its options instead of on an empty selection.
    private int _PageSize => PageSize > 0 ? PageSize : _pageSizeOptions[0];

    // A page size that is not one of the offered ones is offered along with them, in its place among them, since
    // a selector that does not hold the size it reports would show one size while the pagination runs on another.
    private int[] _PageSizeSelectorOptions => Array.IndexOf(_pageSizeOptions, _PageSize) < 0
        ? [.. _pageSizeOptions.Append(_PageSize).Order()]
        : _pageSizeOptions;

    private int _MiddleCount => MiddleCount > 0 ? MiddleCount : DefaultMiddleCount;

    private int _BoundaryCount => BoundaryCount > 0 ? BoundaryCount : DefaultBoundaryCount;

    // The first and last buttons always target a fixed page, so they are the ones the loop leaves alone. The
    // previous and next buttons only stay enabled at the ends of the range while the loop has somewhere else
    // to take them, which a range holding a single page does not.
    private bool _IsFirstDisabled => IsEnabled is false || _SelectedPage == 1;

    private bool _IsPreviousDisabled => IsEnabled is false || (Loop ? _Count == 1 : _SelectedPage == 1);

    private bool _IsNextDisabled => IsEnabled is false || (Loop ? _Count == 1 : _SelectedPage == _Count);

    private bool _IsLastDisabled => IsEnabled is false || _SelectedPage == _Count;

    private int _PreviousPage => Loop && _SelectedPage == 1 ? _Count : _SelectedPage - 1;

    private int _NextPage => Loop && _SelectedPage == _Count ? 1 : _SelectedPage + 1;

    // Every control of the pagination turns into a link at once, so the markup a control renders with never
    // changes under it while the selection moves along the range.
    private bool _UseLinks => GetPageHref is not null;

    // A control that cannot be navigated to carries no address at all, which is what keeps it out of the tab
    // order the way a disabled button is.
    private string? GetHref(int page, bool disabled)
    {
        return disabled ? null : GetPageHref?.Invoke(page);
    }

    private string _VariantClass => Variant switch
    {
        BitVariant.Fill => "bit-pgn-fil",
        BitVariant.Outline => "bit-pgn-otl",
        BitVariant.Text => "bit-pgn-txt",
        _ => "bit-pgn-fil"
    };

    private string GetPageLabel(int page, bool isSelected)
    {
        return GetPageAriaLabel?.Invoke(page, isSelected) ?? $"Page {page}";
    }

    private string GetSummaryText()
    {
        if (GetSummary is not null) return GetSummary(_SelectedPage, _Count);

        // A known number of items is what the position is worth reporting in: "1 - 10 of 240" says how far along
        // the result set the page sits, which the page number on its own does not.
        if (TotalItems > 0)
        {
            var first = (long)(_SelectedPage - 1) * _PageSize + 1;

            return $"{first} - {Math.Min(first + _PageSize - 1, TotalItems)} of {TotalItems}";
        }

        return $"Page {_SelectedPage} of {_Count}";
    }

    private void UpdatePageSizeOptions()
    {
        // A size that is not positive is dropped rather than offered: it would leave the selector picking a
        // page that holds nothing, and the number of pages a total of items adds up to undefined.
        var options = PageSizeOptions?.Where(size => size > 0).ToArray();

        _pageSizeOptions = options is { Length: > 0 } ? options : DefaultPageSizeOptions;
    }

    // A page button is captured when it is inserted, so a page that left the range keeps a reference of an
    // element that is no longer there. Dropping those keeps the pagination from holding one reference per
    // page a long range was walked through; a page that comes back is captured again as it is inserted.
    private void PruneStalePageReferences()
    {
        if (ShowPageButtons is false || _IsHidden)
        {
            _pageRefs.Clear();
            _renderedPages = [];
            return;
        }

        if (_pageRefs.Count <= _renderedPages.Length) return;

        int[] captured = [.. _pageRefs.Keys];

        foreach (var page in captured)
        {
            if (Array.IndexOf(_renderedPages, page) < 0)
            {
                _pageRefs.Remove(page);
            }
        }
    }

    // The gap is a shortcut into the middle of what it hides, which is the page halfway between the two it sits
    // between. A gap always holds at least two pages (a single one is spelled out in its place), so the page the
    // jump lands on is never one of the two already rendered beside it.
    private static int GetEllipsisPage(int[] pages, int index)
    {
        return (pages[index - 1] + pages[index + 1]) / 2;
    }

    // The pages the render is about to lay out are kept so that the references captured for the ones that
    // left the range can be dropped once it is done.
    private int[] GeneratePages()
    {
        return _renderedPages = BuildPages();
    }

    private int[] BuildPages()
    {
        // The size of the window is worked out in long so that boundary and middle counts big enough to
        // overflow an int still compare as wider than the count and fall back to spelling every page out.
        var windowLength = 2L * _BoundaryCount + _MiddleCount + 2;

        if (_Count <= 4 || _Count <= windowLength)
        {
            return Enumerable.Range(1, _Count).ToArray();
        }

        // The window is narrower than the count at this point, so its length fits an int.
        var length = (int)windowLength;
        var pages = new int[length];

        for (var i = 0; i < _BoundaryCount; i++)
        {
            pages[i] = i + 1;
        }

        for (var i = 0; i < _BoundaryCount; i++)
        {
            pages[length - i - 1] = _Count - i;
        }

        int startValue;
        if (_SelectedPage <= _BoundaryCount + _MiddleCount / 2 + 1)
        {
            startValue = _BoundaryCount + 2;
        }
        else if (_SelectedPage >= _Count - _BoundaryCount - _MiddleCount / 2)
        {
            startValue = _Count - _BoundaryCount - _MiddleCount;
        }
        else
        {
            startValue = _SelectedPage - _MiddleCount / 2;
        }

        for (var i = 0; i < _MiddleCount; i++)
        {
            pages[_BoundaryCount + 1 + i] = startValue + i;
        }

        pages[_BoundaryCount] = (_BoundaryCount + _MiddleCount / 2 + 1 < _SelectedPage) ? EllipsisPage : _BoundaryCount + 1;

        pages[length - _BoundaryCount - 1] = (_Count - _BoundaryCount - _MiddleCount / 2 > _SelectedPage) ? EllipsisPage : _Count - _BoundaryCount;

        // An ellipsis standing in for a single page is replaced by that page, since spelling the page out
        // costs the same room as the ellipsis hiding it.
        for (var i = 0; i < length - 2; i++)
        {
            if (pages[i] + 2 == pages[i + 2])
            {
                pages[i + 1] = pages[i] + 1;
            }
        }

        return pages;
    }

    private async Task ChangePage(int page)
    {
        if (IsEnabled is false) return;

        // Every requested page lands inside the available range, so neither a wrapping navigation button nor
        // an out of range SelectedPage can ever select a page that does not exist.
        page = Math.Clamp(page, 1, _Count);

        if (page == _SelectedPage) return;

        await AssignSelectedPage(page);

        // The callback runs even when SelectedPage is bound one way and could not be written back, so that a
        // consumer holding the value itself still hears about the page the user asked for.
        await OnChange.InvokeAsync(page);
    }

    // A click that opens the address somewhere else (another tab, another window, a download) leaves this
    // pagination where it is: the page that was asked for is the one opening over there, and moving the
    // selection here as well would report a page the user never came to.
    private async Task ChangePageFromLink(MouseEventArgs e, int page)
    {
        if (IsModifiedClick(e)) return;

        await ChangePage(page);
    }

    private async Task ChangePageFromLink(MouseEventArgs e, FocusTarget source, int page)
    {
        if (IsModifiedClick(e)) return;

        await ChangePageFrom(source, page);
    }

    private static bool IsModifiedClick(MouseEventArgs e)
    {
        return e.CtrlKey || e.MetaKey || e.ShiftKey || e.AltKey;
    }

    private async Task ChangePageFrom(FocusTarget source, int page)
    {
        if (IsEnabled is false) return;

        var target = ResolveFocusTarget(source, Math.Clamp(page, 1, _Count));

        await ChangePage(page);

        // The focus only moves once the page actually changed, so a click that lands on the page already
        // selected leaves it where the user put it.
        if (target != FocusTarget.None && _SelectedPage == Math.Clamp(page, 1, _Count))
        {
            _focusTarget = target;
        }
    }

    // A navigation button is removed from the tab order the moment the page it moved to disables it, and the
    // focus it was holding goes with it. The page the pagination settles on is the one the focus belongs to,
    // and the navigation button pointing the other way is what is left to hold it once the page buttons are
    // turned off.
    private FocusTarget ResolveFocusTarget(FocusTarget source, int page)
    {
        var stillEnabled = source switch
        {
            FocusTarget.First => page > 1,
            FocusTarget.Previous => Loop ? _Count > 1 : page > 1,
            FocusTarget.Next => Loop ? _Count > 1 : page < _Count,
            FocusTarget.Last => page < _Count,
            _ => true
        };

        if (stillEnabled) return FocusTarget.None;

        if (ShowPageButtons) return FocusTarget.SelectedPage;

        var goingBack = source is FocusTarget.First or FocusTarget.Previous;

        if (goingBack)
        {
            if (ShowNextButton && page < _Count) return FocusTarget.Next;
            if (ShowLastButton && page < _Count) return FocusTarget.Last;
        }
        else
        {
            if (ShowPreviousButton && page > 1) return FocusTarget.Previous;
            if (ShowFirstButton && page > 1) return FocusTarget.First;
        }

        return FocusTarget.None;
    }

    private async Task HandlePageSizeChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;

        if (int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) is false) return;

        if (size == _PageSize) return;

        // The page and the size are read before the size is applied, since the range the page sits in is the one
        // the old size worked out and both of them move as soon as the new size lands.
        var previousSize = _PageSize;
        var previousPage = _SelectedPage;

        var assigned = await AssignPageSize(size);

        // The number of pages follows the page size while the number of items is known, so the page the user was
        // on is no longer the page the items they were looking at are. The selection moves to the page the first
        // of those items landed on, which keeps the reading position instead of the page number. The range only
        // moves along with a size that was actually applied: one bound one way still belongs to the consumer.
        if (assigned && TotalItems > 0)
        {
            var page = Math.Clamp((int)((long)(previousPage - 1) * previousSize / size) + 1, 1, _Count);

            // The page is written back whether or not it moved, since the range it was inside of is not the
            // range that is left and a value the consumer holds must never point outside of it.
            await AssignSelectedPage(page);

            if (page != previousPage)
            {
                await OnChange.InvokeAsync(page);
            }
        }

        // The callback runs even when PageSize is bound one way and could not be written back, so that a
        // consumer holding the value itself still hears about the size the user asked for.
        await OnPageSizeChange.InvokeAsync(size);
    }

    private void HandleGoToPageInput(ChangeEventArgs e)
    {
        _goToPageText = e.Value?.ToString();
    }

    private async Task HandleGoToPageChange(ChangeEventArgs e)
    {
        _goToPageText = e.Value?.ToString();

        // The input clears itself so that the next jump starts from an empty field instead of from the number
        // the previous one left behind. Assigning the text first and clearing it after keeps the two values
        // different, which is what makes the rendered input follow.
        var text = _goToPageText;
        _goToPageText = string.Empty;

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var page) is false) return;

        // A number outside of the range lands on the nearest end of it, since a jump past the last page is a
        // request for the last page and not a typing mistake worth dropping.
        await ChangePage(page);
    }
}
