//-:cnd:noEmit
using AdminPanel.App.Shared;
using AdminPanel.Shared.Dtos.Products;
using Microsoft.AspNetCore.Components;

namespace AdminPanel.App.Pages.Products;
public partial class ProductsPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    public bool IsLoading { get; set; }

    CreateEditProductModal? modal;

    BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };
    BitDataGrid<ProductDto>? dataGrid;
    BitDataGridItemsProvider<ProductDto> productsProvider;

    long TotalCount;
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
                // https://docs.microsoft.com/en-us/odata/concepts/queryoptions-overview

                var query = new Dictionary<string, object>()
                {
                    { "$top", req.Count.HasValue ? req.Count.Value : 10 },
                    { "$skip", req.StartIndex }
                };

                if (string.IsNullOrEmpty(_productNameFilter) is false)
                {
                    query.Add("$filter", $"contains(Name,{_productNameFilter}");
                }

                if (req.GetSortByProperties().Any())
                {
                    query.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var url = navigationManager.GetUriWithQueryParameters("Product/GetProducts", query);

                var data = await httpClient.GetFromJsonAsync(url, AppJsonContext.Default.PagedResultProductDto);

                TotalCount = data!.TotalCount;

                return BitDataGridItemsProviderResult.From(data!.Items, (int)data!.TotalCount);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
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

    private void CreateProduct()
    {
        modal!.ShowModal(new ProductDto());
    }

    private async Task EditProduct(ProductDto product)
    {
        modal!.ShowModal(product);
    }
    private async Task DeleteProduct(ProductDto product)
    {
        if (await ConfirmMessageBox.Show("Are you sure delete?", product.Name, "Delete"))
        {
            await httpClient.DeleteAsync($"Product/Delete/{product.Id}");
            await RefreshData();
        }
    }

    protected async void OnSuccessfulProductSave()
    {
        MessageBox.Show("Succesfully saved", "product");
        await RefreshData();
    }
}




