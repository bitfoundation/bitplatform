//-:cnd:noEmit
using System.Text.Json;
using AdminPanelTemplate.App.Shared;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.App.Pages.Products;
public partial class ProductsPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

    public bool IsLoading { get; set; }

    CreateEditProductModal? modal;

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

                var data= await response.Content.ReadFromJsonAsync(AppJsonContext.Default.PagedResultDtoProductDto);

                NumResults = data!.Total;

                return BitDataGridItemsProviderResult.From(data!.Items, data!.Total);
            }
            catch
            {
                return BitDataGridItemsProviderResult.From<ProductDto>(new List<ProductDto> { }, 0);
            }
            finally
            {
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
        ConfirmMessageBox.Show("Are you sure delete?", product.Name, "Delete", async (confirmed) =>
        {
            if (confirmed)
            {
                await httpClient.DeleteAsync($"Product/{product.Id}");
                await RefreshData();
            }
        });


    }


    protected async void ModalSave()
    {
        MessageBox.Show("Succesfully Added", "product");

        await RefreshData();
    }
}




