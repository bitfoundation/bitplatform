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
    /// This is what the pagination goes on while no <see cref="TotalItems"/> is given: a total number of
    /// items takes over and the number of pages is worked out of it and of <see cref="PageSize"/>.
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
    /// The glyph itself is hidden from assistive technologies and this label is announced in its place, so
    /// the gap in the range is reported as one item instead of being read as a run of punctuation.
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
    /// carries an icon and no text of its own.
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
    /// default text (which reads "Page {number} of {count}", or "Showing {first} to {last} of {total}" while
    /// <see cref="TotalItems"/> is given).
    /// </summary>
    /// <remarks>
    /// This is the hook to localize the summary, or to report the position in terms of the items rather than
    /// the pages (for example "Showing 21 to 30 of 240 results") from numbers only the consumer holds.
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
    /// It names the input on its own, so the visible <see cref="GoToPageText"/> beside it can be dropped
    /// without leaving the input unnamed.
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
    /// carries an icon and no text of its own.
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
    /// carries an icon and no text of its own.
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
    /// </remarks>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The event callback for when the page size is picked out of the page size selector.
    /// </summary>
    /// <remarks>
    /// The callback also runs when <see cref="PageSize"/> is bound one way, and it is where the consumer
    /// recomputes <see cref="Count"/> from the page size it was handed.
    /// </remarks>
    [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }

    /// <summary>
    /// The number of items a page holds, which the page size selector picks.
    /// </summary>
    /// <remarks>
    /// The range of pages follows the picked size on its own while <see cref="TotalItems"/> is given, and
    /// comes from <see cref="Count"/> - which the consumer recomputes from the new size - otherwise.
    /// <br />
    /// A value that is not positive falls back to the first of the <see cref="PageSizeOptions"/>, and the
    /// fallback is written back while the selector is shown, so the size the consumer holds is the one the
    /// selector opens on.
    /// </remarks>
    [Parameter, TwoWayBound]
    public int PageSize { get; set; }

    /// <summary>
    /// The accessible label of the page size selector.
    /// <br />
    /// The default value is <strong>"Items per page"</strong>.
    /// </summary>
    /// <remarks>
    /// It names the selector on its own, so the visible <see cref="PageSizeText"/> beside it can be dropped
    /// without leaving the selector unnamed.
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
    /// carries an icon and no text of its own.
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
    /// Picking a size reports it through <see cref="PageSize"/> and <see cref="OnPageSizeChange"/>. The range
    /// of pages the new size adds up to is rendered on its own while <see cref="TotalItems"/> is given, and is
    /// the consumer's to recompute into <see cref="Count"/> otherwise. A selected page that falls out of the
    /// shrunk range is pulled back either way.
    /// </remarks>
    [Parameter] public bool ShowPageSizeSelector { get; set; }

    /// <summary>
    /// Determines whether to show the previous button.
    /// </summary>
    [Parameter] public bool ShowPreviousButton { get; set; } = true;

    /// <summary>
    /// Shows the position in the range, which reads "Page {number} of {count}" - or
    /// "Showing {first} to {last} of {total}" while <see cref="TotalItems"/> is given - unless
    /// <see cref="GetSummary"/> replaces it, ahead of the buttons of the pagination.
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
    /// The total number of items the pages are made of, which the number of pages is worked out of instead of
    /// being asked for through <see cref="Count"/>.
    /// </summary>
    /// <remarks>
    /// A data source reports how many items it holds, not how many pages they add up to, so handing the total
    /// over is what keeps the arithmetic (and the rounding of the last, partly filled page) out of the
    /// consumer. The number of pages is the total divided by <see cref="PageSize"/>, rounded up, which is why
    /// the page size selector needs nothing else to work: picking a size renders the range the new size adds
    /// up to on its own, and a selected page that falls out of it is pulled back.
    /// <br />
    /// It takes over from <see cref="Count"/> while it is positive, and is ignored otherwise.
    /// <br />
    /// The default summary reports the range of items of the current page instead of its number while the
    /// total is known, since that is the count the total was given for.
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
    /// </remarks>
    public ValueTask FocusAsync()
    {
        // Nothing is rendered at all, so every reference left behind points at an element that is gone.
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
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        // The offered sizes are needed before the first clamp: the page a range holds (and so the range a
        // total number of items adds up to) follows the page size, which falls back to the first of them.
        MaterializePageSizeOptions();

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
        MaterializePageSizeOptions();

        // A page size that is not one the selector can show (a value that was never picked, or one that is
        // not positive) is written back the same way an out of range selected page is, so that the size the
        // consumer holds is the one the selector opens on and the paging math on both sides agrees. Only the
        // rendered selector reports a size, so a pagination without one leaves the value alone.
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



    // A total number of items is what a data source reports, so the number of pages is worked out of it and
    // of the page size (in long, since a total close to the whole range of an int would overflow the rounding
    // up of the last, partly filled page) whenever it is given. There is always at least one page to be on,
    // so a count that is not positive still renders a pagination holding that page instead of an empty one.
    private int _Count => TotalItems > 0
                            ? (int)Math.Min(int.MaxValue, (TotalItems + (long)_PageSize - 1) / _PageSize)
                            : (Count > 0 ? Count : 1);

    // The rendering runs off a clamped view of the selected page so that a value outside of the range (which
    // a one way bound SelectedPage can hold, since the component cannot write it back) still renders a
    // pagination with a current page and with the right buttons disabled.
    private int _SelectedPage => Math.Clamp(SelectedPage, 1, _Count);

    // A page size that was never picked falls back to the first of the offered ones, so the selector opens on
    // a size that is actually one of its options instead of on an empty selection.
    private int _PageSize => PageSize > 0 ? PageSize : _pageSizeOptions[0];

    private int _MiddleCount => MiddleCount > 0 ? MiddleCount : DefaultMiddleCount;

    private int _BoundaryCount => BoundaryCount > 0 ? BoundaryCount : DefaultBoundaryCount;

    // The first and last buttons always target a fixed page, so they are the ones the loop leaves alone. The
    // previous and next buttons only stay enabled at the ends of the range while the loop has somewhere else
    // to take them, which a range holding a single page does not.
    // Navigation that cannot go anywhere is noise, so a single page renders nothing at all while the
    // pagination was asked to hide itself over one.
    private bool _IsHidden => HideOnSinglePage && _Count <= 1;

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

        // A total number of items is only ever given to be reported, so the summary counts the items of the
        // current page rather than repeating the page number the buttons already carry. The last page is
        // only partly filled, which is where the range stops short of a whole page.
        if (TotalItems > 0)
        {
            var first = (_SelectedPage - 1L) * _PageSize + 1;
            var last = Math.Min(first + _PageSize - 1, TotalItems);

            return $"Showing {first} to {last} of {TotalItems}";
        }

        return $"Page {_SelectedPage} of {_Count}";
    }

    private void MaterializePageSizeOptions()
    {
        // A size that is not positive is dropped rather than offered: it would leave the selector picking a
        // page that holds nothing, and the number of pages a total of items adds up to undefined.
        var given = PageSizeOptions?.Where(size => size > 0).ToArray();

        var options = given is { Length: > 0 } ? given : DefaultPageSizeOptions;

        // A picked size the list does not hold is offered along with the others, where it belongs among them
        // while they run up. A select falls back to its first option otherwise, which would leave the selector
        // showing another size than the one the pagination is actually paging by.
        if (PageSize > 0 && Array.IndexOf(options, PageSize) < 0)
        {
            var index = 0;
            while (index < options.Length && options[index] < PageSize) index++;

            var merged = new int[options.Length + 1];
            Array.Copy(options, merged, index);
            merged[index] = PageSize;
            Array.Copy(options, index, merged, index + 1, options.Length - index);

            options = merged;
        }

        _pageSizeOptions = options;
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

        await AssignPageSize(size);

        // The list of offered sizes holds the picked one, so a size that was only offered because it was
        // picked before is dropped from it again as soon as it is not.
        MaterializePageSizeOptions();

        // A bigger page holds the same items in fewer pages, and the range the new size adds up to can stop
        // short of the selected page. The pagination works that range out on its own while it is given a
        // total number of items, so it is also the one to pull the selection back into it.
        if (SelectedPage != _SelectedPage)
        {
            await AssignSelectedPage(_SelectedPage);
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
