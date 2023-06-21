namespace AdminPanel.Shared.Dtos;

public class PagedResult<T>
{
    public IList<T> Items { get; set; }

    public long TotalCount { get; set; }

    public PagedResult(IList<T> items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public PagedResult()
    {

    }
}
