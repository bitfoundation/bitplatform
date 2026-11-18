using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A headless pagination: a <c>&lt;nav&gt;</c> landmark (named "Pagination") with page buttons - or links, for
/// pages that have their own address - previous / next / first / last buttons, and a page range that keeps the
/// first and last pages and the ones around the current page in view, with gaps for the rest.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Page</c> (1-based), or uncontrolled with <see cref="DefaultPage"/>.</item>
///   <item><b>Page count</b> — <see cref="PageCount"/>, or <see cref="TotalItems"/> and <see cref="PageSize"/>.</item>
///   <item><b>Range</b> — <see cref="SiblingCount"/> pages on each side of the current one, <see cref="BoundaryCount"/>
///     at each end, the rest collapsed into <see cref="BitBhPaginationEllipsis"/> entries (the same algorithm as MUI).</item>
///   <item><b>Links</b> — with <see cref="PageHref"/> every part renders an <c>&lt;a href&gt;</c>, so pages can be opened in a
///     new tab and work without interactivity.</item>
///   <item><b>ARIA</b> — the current page has <c>aria-current="page"</c>; page items are named "Page 4".</item>
/// </list>
///
/// <code>
/// &lt;BitBhPagination @bind-Page="page" TotalItems="230" PageSize="10" class="pagination"&gt;
///     &lt;Template Context="p"&gt;
///         &lt;BitBhPaginationPrevious&gt;‹&lt;/BitBhPaginationPrevious&gt;
///         @foreach (var entry in p.Entries)
///         {
///             if (entry.IsEllipsis) { &lt;BitBhPaginationEllipsis @key="entry.Key"&gt;…&lt;/BitBhPaginationEllipsis&gt; }
///             else { &lt;BitBhPaginationItem @key="entry.Key" Page="entry.Page"&gt;@entry.Page&lt;/BitBhPaginationItem&gt; }
///         }
///         &lt;BitBhPaginationNext&gt;›&lt;/BitBhPaginationNext&gt;
///     &lt;/Template&gt;
/// &lt;/BitBhPagination&gt;
/// </code>
/// </summary>
public class BitBhPagination : BitBhComponentBase
{
    private int _page = 1;
    private BitBhPaginationContext? _context;

    /// <summary>The current page (1-based). Supplying it makes the pagination controlled (use <c>@bind-Page</c>).</summary>
    [Parameter]
    public int Page { get; set; }

    /// <summary>Two-way binding callback for <see cref="Page"/>.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>Initial page when uncontrolled. Default: 1.</summary>
    [Parameter]
    public int DefaultPage { get; set; } = 1;

    /// <summary>Fires whenever the page changes.</summary>
    [Parameter]
    public EventCallback<int> OnPageChange { get; set; }

    /// <summary>The number of pages. Takes precedence over <see cref="TotalItems"/> / <see cref="PageSize"/>.</summary>
    [Parameter]
    public int? PageCount { get; set; }

    /// <summary>The number of items, with <see cref="PageSize"/>: the page count is computed from them.</summary>
    [Parameter]
    public int TotalItems { get; set; }

    /// <summary>Items per page, with <see cref="TotalItems"/>. Default: 10.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Pages shown on each side of the current page. Default: 1.</summary>
    [Parameter]
    public int SiblingCount { get; set; } = 1;

    /// <summary>Pages always shown at the start and at the end. Default: 1.</summary>
    [Parameter]
    public int BoundaryCount { get; set; } = 1;

    /// <summary>
    /// The address of a page: every part renders a link to it instead of a button. The pagination doesn't change
    /// the page itself then - the page the link opens does (bind <see cref="Page"/> to the route).
    /// </summary>
    [Parameter]
    public Func<int, string>? PageHref { get; set; }

    /// <summary>Disables every part.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The parts, when they don't depend on the page range.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the pagination's state (<see cref="BitBhPaginationContext.Entries"/> …).</summary>
    [Parameter]
    public RenderFragment<BitBhPaginationContext>? Template { get; set; }

    protected override string DefaultTag => "nav";

    /// <summary>The number of pages (at least 1).</summary>
    public int TotalPages => Math.Max(1, PageCount ?? (PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 1));

    /// <summary>The current page, within 1 and <see cref="TotalPages"/>.</summary>
    public int CurrentPage => Math.Clamp(IsProvided(nameof(Page)) ? Page : _page, 1, TotalPages);

    protected override void OnInitialized() => _page = DefaultPage;

    /// <summary>Goes to <paramref name="page"/> (clamped).</summary>
    public async Task GoToAsync(int page)
    {
        page = Math.Clamp(page, 1, TotalPages);
        if (Disabled || page == CurrentPage) return;
        if (!IsProvided(nameof(Page))) _page = page;
        await PageChanged.InvokeAsync(page);
        await OnPageChange.InvokeAsync(page);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhPaginationContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhPaginationContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (!Tag.Equals("nav", StringComparison.OrdinalIgnoreCase)) attrs["role"] = "navigation";
        attrs["aria-label"] = Strings.Pagination;
        SetDataFlag(attrs, "disabled", Disabled);
        SetDataValue(attrs, "page", CurrentPage.ToString(CultureInfo.InvariantCulture));
        SetDataValue(attrs, "page-count", TotalPages.ToString(CultureInfo.InvariantCulture));
        return attrs;
    }

    /// <summary>
    /// The page range for <paramref name="page"/> of <paramref name="count"/> pages: <paramref name="boundary"/> pages at each
    /// end, <paramref name="siblings"/> around the current one, gaps (page 0) between. The number of entries stays the same
    /// as the page moves, so the controls don't jump around (MUI's algorithm).
    /// </summary>
    public static IReadOnlyList<BitBhPaginationEntry> Range(int page, int count, int siblings = 1, int boundary = 1)
    {
        count = Math.Max(1, count);
        page = Math.Clamp(page, 1, count);
        siblings = Math.Max(0, siblings);
        boundary = Math.Max(0, boundary);

        static IEnumerable<int> Span(int start, int end) => end < start ? [] : Enumerable.Range(start, end - start + 1);

        var startPages = Span(1, Math.Min(boundary, count)).ToList();
        var endPages = Span(Math.Max(count - boundary + 1, boundary + 1), count).ToList();
        var siblingsStart = Math.Max(Math.Min(page - siblings, count - boundary - siblings * 2 - 1), boundary + 2);
        var siblingsEnd = Math.Min(Math.Max(page + siblings, boundary + siblings * 2 + 2), endPages.Count > 0 ? endPages[0] - 2 : count - 1);

        var entries = new List<BitBhPaginationEntry>();
        void AddPage(int p) => entries.Add(new BitBhPaginationEntry(p, $"p{p.ToString(CultureInfo.InvariantCulture)}"));

        startPages.ForEach(AddPage);
        if (siblingsStart > boundary + 2) entries.Add(new BitBhPaginationEntry(0, "start-ellipsis"));
        else if (boundary + 1 < count - boundary) AddPage(boundary + 1);
        foreach (var p in Span(siblingsStart, siblingsEnd)) AddPage(p);
        if (siblingsEnd < count - boundary - 1) entries.Add(new BitBhPaginationEntry(0, "end-ellipsis"));
        else if (count - boundary > boundary) AddPage(count - boundary);
        endPages.ForEach(AddPage);

        // Small counts make the parts overlap: keep each page once, in order.
        var seen = new HashSet<int>();
        return entries.Where(e => e.IsEllipsis || (e.Page >= 1 && e.Page <= count && seen.Add(e.Page))).ToList();
    }
}
