using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanel.Shared.Dtos;

[Serializable]
public class PagedResultDto<T> where T : class
{
    private IList<T> _items;
    public IList<T> Items
    {
        get { return _items ??= new List<T>(); }
        set { _items = value; }
    }


    public int Total { get; set; }


    public PagedResultDto(IList<T> items, int total)
    {
        Items = items;
        Total = total;
    }

    public PagedResultDto()
    {

    }

}
