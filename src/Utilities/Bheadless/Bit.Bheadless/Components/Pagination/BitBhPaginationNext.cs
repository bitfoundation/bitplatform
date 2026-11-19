namespace Bit.Bheadless;

/// <summary>Goes to the next page of a <see cref="BitBhPagination"/> (named "Next page"; disabled on the last page).</summary>
public class BitBhPaginationNext : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { } p && p.CurrentPage < p.TotalPages ? p.CurrentPage + 1 : null;

    protected override string? DefaultLabel => Strings.NextPage;
}
