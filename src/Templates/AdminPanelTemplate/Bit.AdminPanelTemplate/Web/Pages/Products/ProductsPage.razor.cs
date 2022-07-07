//-:cnd:noEmit
using System.Text.Json;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.App.Pages.Products;
public partial class ProductsPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<ProductDto>? dataGrid;
    BitDataGridItemsProvider<ProductDto> productsProvider;

    int NumResults;
    string _productNameFilter = string.Empty;
    string ProductNameFilter
    {
        get => _productNameFilter;
        set
        {
            _productNameFilter = value;
            _ = dataGrid.RefreshDataAsync();
        }
    }



    protected override async Task OnInitAsync()
    {
        await PrepareGridDataProvider();
        await base.OnInitAsync();
    }

    private async Task PrepareGridDataProvider()
    {
        productsProvider = async req =>
        {
            try
            {
                var input = new PagedInputDto()
                {
                    Skip = req.StartIndex,
                    MaxResultCount = (req.Count.HasValue) ? req.Count.Value : 10,
                    Filter = _productNameFilter,
                    SortBy = req.SortByColumn?.Title,
                    SortAscending = req.SortByAscending
                };

                var response = await httpClient.PostAsJsonAsync("Product/GetProducts", input, AppJsonContext.Default.PagedInputDto);

                var data = JsonSerializer.Deserialize(response.Content.ReadAsStream(), AppJsonContext.Default.PagedResultDtoProductDto);

                NumResults = data!.Total;

                return BitDataGridItemsProviderResult.From(data!.Items, data!.Total);
            }
            catch(Exception ex)
            {
                return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
            }   
            finally
            {
                StateHasChanged();
            }

        };

    }
}




