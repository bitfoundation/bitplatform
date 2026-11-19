namespace Bit.Bheadless;

/// <summary>Goes to the last page of a <see cref="BitBhPagination"/> (named "Last page"; disabled on it).</summary>
public class BitBhPaginationLast : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { } p && p.CurrentPage < p.TotalPages ? p.TotalPages : null;

    protected override string? DefaultLabel => Strings.LastPage;
}
