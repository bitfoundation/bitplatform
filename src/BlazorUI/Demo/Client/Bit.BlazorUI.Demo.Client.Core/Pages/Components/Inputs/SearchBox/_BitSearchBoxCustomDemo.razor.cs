namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SearchBox;

public partial class _BitSearchBoxCustomDemo
{
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;

    private string TwoWaySearchValue;
    private string OnChangeSearchValue;
    private string OnSearchValue;
    private string SearchValue;
    private string ItemsProviderSearchValue;

    private readonly ValidationSearchBoxModel ValidationSearchBoxModel = new();

    private List<BitSearchBoxCustom> GetBasicCustoms() => new()
    {
        new() { Text = "Apple" },
        new() { Text = "Banana" },
        new() { Text = "Orange" },
        new() { Text = "Grape" },
        new() { Text = "Broccoli" },
        new() { Text = "Carrot" },
        new() { Text = "Lettuce" }
    };


    private BitSearchBoxNameSelectors<BitSearchBoxCustom> nameSelectors = new()
    {
        AriaLabel = { Selector = c => c.Label },
        Class = { Selector = c => c.CssClass },
        Id = { Selector = c => c.Key },
        IsSelected = { Name = nameof(BitSearchBoxCustom.IsSelected) },
        Style = { Selector = c => c.CssStyle },
        Text = { Selector = c => c.Text },
        Title = { Selector = c => c.Title },
    };

    private async ValueTask<BitSearchBoxItemsProviderResult<BitSearchBoxCustom>> LoadItems(BitSearchBoxItemsProviderRequest<BitSearchBoxCustom> request)
    {
        try
        {
            // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

            var query = new Dictionary<string, object?>()
            {
                { "$top", request.Count == 0 ? 5 : request.Count },
                { "$skip", request.StartIndex }
            };

            if (string.IsNullOrEmpty(request.Search) is false)
            {
                query.Add("$filter", $"contains(Name,'{request.Search}')");
            }

            var url = NavManager.GetUriWithQueryParameters("Products/GetProducts", query);

            var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

            var items = data!.Items.Select(i => new BitSearchBoxCustom
            {
                Text = i.Name,
                Label = i.Name,
            }).ToList();

            return BitSearchBoxItemsProviderResult.From(items, data!.TotalCount);
        }
        catch
        {
            return BitSearchBoxItemsProviderResult.From(new List<BitSearchBoxCustom>(), 0);
        }
    }
}
