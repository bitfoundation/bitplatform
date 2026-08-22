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
/// </remarks>
public partial class BitPagination : BitComponentBase
{
    /// <summary>
    /// The placeholder a generated page list uses in place of the pages that are collapsed into an ellipsis.
    /// </summary>
    private const int EllipsisPage = -1;

    private const int DefaultMiddleCount = 3;
    private const int DefaultBoundaryCount = 2;

    private int _correctedPage;



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
    /// </remarks>
    [Parameter] public int Count { get; set; }

    /// <summary>
    /// The default selected page number.
    /// </summary>
    [Parameter] public int DefaultSelectedPage { get; set; }

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
    /// Provides the text of the summary, from the selected page and the total number of pages, replacing the
    /// default "Page {number} of {count}" text.
    /// </summary>
    /// <remarks>
    /// This is the hook to localize the summary, or to report the position in terms of the items rather than
    /// the pages (for example "Showing 21 to 30 of 240 results") from numbers only the consumer holds.
    /// <br />
    /// It is only called while <see cref="ShowSummary"/> is on.
    /// </remarks>
    [Parameter] public Func<int, int, string>? GetSummary { get; set; }

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
    /// The event callback for when selected page changes.
    /// </summary>
    /// <remarks>
    /// The callback also runs when <see cref="SelectedPage"/> is bound one way, so a page can be requested
    /// and applied by the consumer without giving up control of the value.
    /// </remarks>
    [Parameter] public EventCallback<int> OnChange { get; set; }

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
    /// Determines whether to show the previous button.
    /// </summary>
    [Parameter] public bool ShowPreviousButton { get; set; } = true;

    /// <summary>
    /// Shows the position in the range, which reads "Page {number} of {count}" unless
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
    /// The visual variant of the pagination.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



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



    // There is always at least one page to be on, so a count that is not positive still renders a pagination
    // holding that page instead of an empty one.
    private int _Count => Count > 0 ? Count : 1;

    // The rendering runs off a clamped view of the selected page so that a value outside of the range (which
    // a one way bound SelectedPage can hold, since the component cannot write it back) still renders a
    // pagination with a current page and with the right buttons disabled.
    private int _SelectedPage => Math.Clamp(SelectedPage, 1, _Count);

    private int _MiddleCount => MiddleCount > 0 ? MiddleCount : DefaultMiddleCount;

    private int _BoundaryCount => BoundaryCount > 0 ? BoundaryCount : DefaultBoundaryCount;

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
        return GetSummary?.Invoke(_SelectedPage, _Count) ?? $"Page {_SelectedPage} of {_Count}";
    }

    private int[] GeneratePages()
    {
        if (_Count <= 4 || _Count <= 2 * _BoundaryCount + _MiddleCount + 2)
        {
            return Enumerable.Range(1, _Count).ToArray();
        }

        var length = 2 * _BoundaryCount + _MiddleCount + 2;
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
}
