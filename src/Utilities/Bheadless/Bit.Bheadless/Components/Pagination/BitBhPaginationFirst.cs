namespace Bit.Bheadless;

/// <summary>Goes to the first page of a <see cref="BitBhPagination"/> (named "First page"; disabled on it).</summary>
public class BitBhPaginationFirst : BitBhPaginationTargetBase
{
    protected override int? TargetPage => Pagination is { CurrentPage: > 1 } ? 1 : null;

    protected override string? DefaultLabel => Strings.FirstPage;
}
