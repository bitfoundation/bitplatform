namespace Bit.Bheadless;

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
