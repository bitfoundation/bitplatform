using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

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
