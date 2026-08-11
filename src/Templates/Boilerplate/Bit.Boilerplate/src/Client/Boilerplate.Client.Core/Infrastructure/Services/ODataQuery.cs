namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
/// </summary>
public partial class ODataQuery
{
    public int? Top { get; set; }

    public int? Skip { get; set; }

    public string? Filter { get; set; }

    /// <remarks>
    /// Both operands are parenthesized: OData binds `and` tighter than `or`, so appending ` and X` to a filter that
    /// already contains a top-level `or` would re-associate it - `A or B and C` is `A or (B and C)` - and the server
    /// would silently answer with a superset of the intended rows.
    /// </remarks>
    public string AndFilter
    {
        set => Filter = string.IsNullOrEmpty(Filter) ? value : $"({Filter}) and ({value})";
    }

    /// <remarks>
    /// <inheritdoc cref="AndFilter"/>
    /// </remarks>
    public string OrFilter
    {
        set => Filter = string.IsNullOrEmpty(Filter) ? value : $"({Filter}) or ({value})";
    }

    public string? OrderBy { get; set; }
    public string? Select { get; set; }
    public string? Expand { get; set; }
    public string? Search { get; set; }

    public override string? ToString()
    {
        var qs = new AppQueryStringCollection();

        if (Top is not null)
        {
            qs.Add("$top", Top.ToString()!);
        }

        if (Skip is not null)
        {
            qs.Add("$skip", Skip.ToString()!);
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

        return qs.ToString();
    }

    public static implicit operator string?(ODataQuery query) => query.ToString();
}
