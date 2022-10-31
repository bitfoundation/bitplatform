//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Client.Shared.Pages.Products;

[Authorize]
public partial class ProductsPage
{
    public bool IsLoading { get; set; }

    CreateEditProductModal? modal;

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<ProductDto>? dataGrid;
    BitDataGridItemsProvider<ProductDto> productsProvider;

    string _productNameFilter = string.Empty;
    string ProductNameFilter
    {
        get => _productNameFilter;
        set
        {
            _productNameFilter = value;
            _ = dataGrid!.RefreshDataAsync();
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
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

                var query = new Dictionary<string, object>()
                {
                    { "$top", req.Count.HasValue ? req.Count.Value : 10 },
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
                IsLoading = false;
                StateHasChanged();
            }
        };
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshDataAsync();
    }

    private async Task CreateProduct()
    {
        await modal!.ShowModal(new ProductDto());
    }

    private async Task EditProduct(ProductDto product)
    {
        await modal!.ShowModal(product);
    }

    private async Task DeleteProduct(ProductDto product)
    {
        var confirmed = await ConfirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteProduct), product.Name!), Localizer[nameof(AppStrings.DeleteProduct)]);

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Product/Delete/{product.Id}");
            await RefreshData();
        }
    }
}




