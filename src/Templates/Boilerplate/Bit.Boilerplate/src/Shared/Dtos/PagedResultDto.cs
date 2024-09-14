namespace Boilerplate.Shared.Dtos;

public partial class PagedResult<T>
{
    public T[] Items { get; set; } = [];

    public long TotalCount { get; set; }

    [JsonConstructor]
    public PagedResult(T[] items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
