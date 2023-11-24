//-:cnd:noEmit
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Pages.Products;

[Authorize]
public partial class ProductsPage
{
    private bool isLoading;
    private AddOrEditProductModal? modal;
    private string productNameFilter = string.Empty;

    private ConfirmMessageBox confirmMessageBox = default!;
    private BitDataGrid<ProductDto>? dataGrid;
    private BitDataGridItemsProvider<ProductDto> productsProvider = default!;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };

    string ProductNameFilter
    {
        get => productNameFilter;
        set
        {
            productNameFilter = value;
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
        productsProvider = async req =>
        {
            isLoading = true;

            try
            {
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview
                var query = new Dictionary<string, object?>()
                {
                    { "$top", req.Count ?? 10 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(productNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,'{productNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = NavigationManager.GetUriWithQueryParameters("Product/GetProducts", query);

                var data = await HttpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                return BitDataGridItemsProviderResult.From(await data!.Items!.ToListAsync(), (int)data!.TotalCount);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
                return BitDataGridItemsProviderResult.From(new List<ProductDto> { }, 0);
            }
            finally
            {
                isLoading = false;

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
        var confirmed = await confirmMessageBox.Show(Localizer.GetString(nameof(AppStrings.AreYouSureWannaDeleteProduct), product.Name ?? string.Empty),
                                                     Localizer[nameof(AppStrings.DeleteProduct)]);

        if (confirmed)
        {
            await HttpClient.DeleteAsync($"Product/Delete/{product.Id}");

            await RefreshData();
        }
    }
}




