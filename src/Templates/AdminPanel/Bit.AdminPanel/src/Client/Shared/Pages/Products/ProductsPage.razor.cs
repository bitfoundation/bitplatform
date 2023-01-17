//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Client.Shared.Pages;

[Authorize]
public partial class ProductsPage
{
    private bool _isLoading;
    private CreateEditProductModal? _modal;
    private string _productNameFilter = string.Empty;


    private BitDataGrid<ProductDto>? _dataGrid;
    private BitDataGridItemsProvider<ProductDto> _productsProvider = default!;
    private BitDataGridPaginationState _pagination = new() { ItemsPerPage = 10 };

    string ProductNameFilter
    {
        get => _productNameFilter;
        set
        {
            _productNameFilter = value;
            _ = RefreshData();
        }
    }

    protected override async Task OnInitAsync()
    {
        PrepareGridDataProvider();

        await base.OnInitAsync();
    }

    private void PrepareGridDataProvider()
    {
        _productsProvider = async req =>
        {
            _isLoading = true;

            try
            {
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count ?? 10 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(_productNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,'{_productNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = NavigationManager.GetUriWithQueryParameters("Product/GetProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From(new List<ProductDto> { }, 0);
            }
            finally
            {
                _isLoading = false;

                StateHasChanged();
            }
        };
    }

    private async Task RefreshData()
    {
        await _dataGrid!.RefreshDataAsync();
    }

    private async Task CreateProduct()
    {
        await _modal!.ShowModal(new ProductDto());
    }

    private async Task EditProduct(ProductDto product)
    {
        await _modal!.ShowModal(product);
    }

    private async Task DeleteProduct(ProductDto product)
    {
        var confirmed = await ConfirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteProduct), product.Name ?? string.Empty), 
                                                     Localizer[nameof(AppStrings.DeleteProduct)]);

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Product/Delete/{product.Id}");

            await RefreshData();
        }
    }
}




