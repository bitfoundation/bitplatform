using System.Web;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
/// </summary>
public partial class ODataQuery
{
    public int? Top { get; set; }

    public int? Skip { get; set; }

    public string? Filter { get; set; }

    public string AndFilter
    {
        set => Filter = Filter != null ? $"{Filter} and {value}" : value;
    }

    public string OrFilter
    {
        set => Filter = Filter != null ? $"{Filter} or {value}" : value;
    }

    public string? OrderBy { get; set; }
    public string? Select { get; set; }
    public string? Expand { get; set; }
    public string? Search { get; set; }

    public override string ToString()
    {
        var qs = HttpUtility.ParseQueryString(string.Empty);

        if (Top is not null)
        {
            qs.Add("$top", Top.ToString());
        }

        if (Skip is not null)
        {
            qs.Add("$skip", Skip.ToString());
        }

        if (string.IsNullOrEmpty(Filter) is false)
        {
            qs.Add("$filter", Filter);
        }

        if (string.IsNullOrEmpty(OrderBy) is false)
        {
            qs.Add("$orderby", OrderBy);
        }

        if (string.IsNullOrEmpty(Select) is false)
        {
            qs.Add("$select", Select);
        }

        if (string.IsNullOrEmpty(Expand) is false)
        {
            qs.Add("$expand", Expand);
        }

        if (string.IsNullOrEmpty(Search) is false)
        {
            qs.Add("$search", Search);
        }

        return qs.ToString()!;
    }

    public static implicit operator string(ODataQuery query) => query.ToString();
}
