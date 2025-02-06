//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Products;

public partial class ProductsPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Products)];
    protected override string? Subtitle => string.Empty;


    [AutoInject] IProductController productController = default!;


    private bool isLoading;
    private bool isDeleteDialogOpen;
    private ProductDto? deletingProduct;
    private AddOrEditProductModal? modal;
    private string productNameFilter = string.Empty;
    private string categoryNameFilter = string.Empty;

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

    string CategoryNameFilter
    {
        get => categoryNameFilter;
        set
        {
            categoryNameFilter = value;
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
                var odataQ = new ODataQuery
                {
                    Top = req.Count ?? 10,
                    Skip = req.StartIndex,
                    OrderBy = string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                };

                if (string.IsNullOrEmpty(ProductNameFilter) is false)
                {
                    odataQ.Filter = $"contains(tolower({nameof(ProductDto.Name)}),'{ProductNameFilter.ToLower()}')";
                }

                if (string.IsNullOrEmpty(CategoryNameFilter) is false)
                {
                    odataQ.AndFilter = $"contains(tolower({nameof(ProductDto.CategoryName)}),'{CategoryNameFilter.ToLower()}')";
                }

                var data = await productController.WithQueryString(odataQ.ToString()).GetProducts(req.CancellationToken);

                return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
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

    private async Task DeleteProduct()
    {
        if (deletingProduct is null) return;

        try
        {
            await productController.Delete(deletingProduct.Id, deletingProduct.ConcurrencyStamp.ToStampString(), CurrentCancellationToken);

            await RefreshData();
        }
        finally
        {
            deletingProduct = null;
        }
    }
}




