namespace Bit.BlazorUI.Demo.Shared.Dtos;

public class PagedResult<T>
{
    public IList<T>? Items { get; set; }

    public int TotalCount { get; set; }

    public PagedResult(IList<T> items, int totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }

    public PagedResult()
    {

    }
}
