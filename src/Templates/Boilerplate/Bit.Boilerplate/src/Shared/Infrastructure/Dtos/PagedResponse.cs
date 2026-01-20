namespace Boilerplate.Shared.Infrastructure.Dtos;

public partial class PagedResponse<T>
{
    public T[] Items { get; set; } = [];

    public long TotalCount { get; set; }

    [JsonConstructor]
    public PagedResponse(T[] items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
