using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdminPanelTemplate.Shared.Dtos;
public class PagedInputDto
{
    public int Skip { get; set; }

    public int Limit { get; set; } = 10;

    public string? SortBy { get; set; }

    public bool SortAscending { get; set; } = true;

    public string Filter { get; set; } = "";

    public CancellationToken CancellationToken { get; set; }

}
