using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Shared.Dtos;

public class PagedResult<T>
{
    public IList<T> Items { get; set; }

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
