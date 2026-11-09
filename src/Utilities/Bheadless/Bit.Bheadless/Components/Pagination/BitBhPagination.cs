using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>One entry of a pagination's page range: a page number, or a gap (an ellipsis).</summary>
/// <param name="Page">The page number (1-based), or 0 for a gap.</param>
/// <param name="Key">A key that stays stable for the entry while the page changes (<c>@key</c> it).</param>
public sealed record BitBhPaginationEntry(int Page, string Key)
{
    /// <summary>Whether the entry is a gap (an ellipsis) rather than a page.</summary>
    public bool IsEllipsis => Page == 0;
}

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

/// <summary>The state of a <see cref="BitBhPagination"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhPaginationContext
{
    internal BitBhPaginationContext(BitBhPagination owner) => Owner = owner;

    internal BitBhPagination Owner { get; }

    /// <summary>The current page (1-based).</summary>
    public int Page => Owner.CurrentPage;

    /// <summary>The number of pages.</summary>
    public int PageCount => Owner.TotalPages;

    /// <summary>The page range to render: pages and gaps.</summary>
    public IReadOnlyList<BitBhPaginationEntry> Entries => BitBhPagination.Range(Owner.CurrentPage, Owner.TotalPages, Owner.SiblingCount, Owner.BoundaryCount);

    /// <summary>Whether there is a page before the current one.</summary>
    public bool HasPrevious => Owner.CurrentPage > 1;

    /// <summary>Whether there is a page after the current one.</summary>
    public bool HasNext => Owner.CurrentPage < Owner.TotalPages;

    /// <summary>The first item of the current page (1-based), for "Showing 11–20 of 230".</summary>
    public int FirstItem => Owner.TotalItems == 0 ? 0 : (Owner.CurrentPage - 1) * Owner.PageSize + 1;

    /// <summary>The last item of the current page, for "Showing 11–20 of 230".</summary>
    public int LastItem => Math.Min(Owner.CurrentPage * Owner.PageSize, Owner.TotalItems);

    /// <summary>Goes to a page.</summary>
    public Task GoToAsync(int page) => Owner.GoToAsync(page);
}

/// <summary>
/// Shared behaviour of the pagination parts that go to a page: a button, or a link when the pagination has a
/// <see cref="BitBhPagination.PageHref"/>. A part that can't go anywhere (Previous on the first page) is disabled -
/// a link without an <c>href</c> and <c>aria-disabled</c>.
/// </summary>
public abstract class BitBhPaginationTargetBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhPaginationContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The owning pagination.</summary>
    protected BitBhPagination? Pagination => Context?.Owner;

    private bool IsLink => Pagination?.PageHref is not null && As is null;

    protected override string DefaultTag => IsLink ? "a" : "button";

    /// <summary>The page this part goes to, or null when it can't go anywhere.</summary>
    protected abstract int? TargetPage { get; }

    /// <summary>The accessible name given to the part (an <c>aria-label</c> you set replaces it).</summary>
    protected abstract string? DefaultLabel { get; }

    /// <summary>Whether this part stands for the current page.</summary>
    protected virtual bool IsCurrent => false;

    private bool IsDisabled => Pagination is null || Pagination.Disabled || TargetPage is null;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (!IsLink)
            builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => !IsDisabled && TargetPage is { } page && Pagination is { } p ? p.GoToAsync(page) : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled && !IsCurrent) attrs["disabled"] = true;
        }
        else if (Tag.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
            if (!disabled && TargetPage is { } page && Pagination?.PageHref is { } href) attrs["href"] = href(page);
            else
            {
                attrs["role"] = "link";
                attrs["aria-disabled"] = "true";
            }
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        if (DefaultLabel is { } label) attrs["aria-label"] = label;
        if (IsCurrent) attrs["aria-current"] = "page";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled && !IsCurrent);
        SetDataFlag(attrs, "selected", IsCurrent);
        return attrs;
    }
}

/// <summary>A page of a <see cref="BitBhPagination"/>, named "Page 4"; the current one has <c>aria-current="page"</c> and <c>data-selected</c>.</summary>
public class BitBhPaginationItem : BitBhPaginationTargetBase
{
    /// <summary>The page (1-based).</summary>
    [Parameter, EditorRequired]
    public int Page { get; set; }

    protected override int? TargetPage => Pagination is { } p && Page >= 1 && Page <= p.TotalPages ? Page : null;

    protected override string? DefaultLabel => Strings.Page(Page);

    protected override bool IsCurrent => Pagination?.CurrentPage == Page;

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "page", Page.ToString(CultureInfo.InvariantCulture));
        return attrs;
    }
}

/// <summary>Goes to the previous page of a <see cref="BitBhPagination"/> (named "Previous page"; disabled on the first page).</summary>
public class BitBhPaginationPrevious : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { CurrentPage: > 1 } p ? p.CurrentPage - 1 : null;

    protected override string? DefaultLabel => Strings.PreviousPage;
}

/// <summary>Goes to the next page of a <see cref="BitBhPagination"/> (named "Next page"; disabled on the last page).</summary>
public class BitBhPaginationNext : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { } p && p.CurrentPage < p.TotalPages ? p.CurrentPage + 1 : null;

    protected override string? DefaultLabel => Strings.NextPage;
}

/// <summary>Goes to the first page of a <see cref="BitBhPagination"/> (named "First page"; disabled on it).</summary>
public class BitBhPaginationFirst : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { CurrentPage: > 1 } ? 1 : null;

    protected override string? DefaultLabel => Strings.FirstPage;
}

/// <summary>Goes to the last page of a <see cref="BitBhPagination"/> (named "Last page"; disabled on it).</summary>
public class BitBhPaginationLast : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { } p && p.CurrentPage < p.TotalPages ? p.TotalPages : null;

    protected override string? DefaultLabel => Strings.LastPage;
}

/// <summary>A gap in a <see cref="BitBhPagination"/>'s page range ("…"), hidden from assistive technology.</summary>
public class BitBhPaginationEllipsis : BitBhComponentBase
{
    /// <summary>The content. Default: "…".</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        if (ChildContent is null) builder.AddContent(30, "…");
        else builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        return attrs;
    }
}
