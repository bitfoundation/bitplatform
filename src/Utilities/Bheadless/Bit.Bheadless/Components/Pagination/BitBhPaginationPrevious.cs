namespace Bit.Bheadless;

/// <summary>Goes to the previous page of a <see cref="BitBhPagination"/> (named "Previous page"; disabled on the first page).</summary>
public class BitBhPaginationPrevious : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { CurrentPage: > 1 } p ? p.CurrentPage - 1 : null;

    protected override string? DefaultLabel => Strings.PreviousPage;
}
