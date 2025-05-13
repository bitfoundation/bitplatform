//-:cnd:noEmit
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Products;

public partial class ProductsPage
{
    private bool isLoading;
    private bool isSmallScreen;
    private string? searchQuery;
    private bool isDeleteDialogOpen;
    private ProductDto? deletingProduct;
    private string productNameFilter = string.Empty;
    private string categoryNameFilter = string.Empty;

    private BitDataGrid<ProductDto>? dataGrid;
    private BitDataGridItemsProvider<ProductDto> productsProvider = default!;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };


    [AutoInject] IProductController productController = default!;


    private string ProductNameFilter
    {
        get => productNameFilter;
        set
        {
            productNameFilter = value;
            _ = RefreshData();
        }
    }

    private string CategoryNameFilter
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
        await base.OnInitAsync();

        PrepareGridDataProvider();
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

                var queriedRequest = productController.WithQuery(odataQ.ToString());
                var data = await (string.IsNullOrWhiteSpace(searchQuery)
                                    ? queriedRequest.GetProducts(req.CancellationToken)
                                    : queriedRequest.GetProductsBySearchQuery(searchQuery, req.CancellationToken));

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
        NavigationManager.NavigateTo(Urls.AddOrEditProductPage);
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

    private async Task HandleOnSearch(string value)
    {
        searchQuery = value;
        await RefreshData();
    }
}




