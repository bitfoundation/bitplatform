//+:cnd:noEmit
using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Pages.Products;

public partial class ProductsPage
{
    private bool isLoading;
    private bool isSmallScreen;
    private string? searchQuery;
    private bool isDeleteDialogOpen;
    private ProductDto? deletingProduct;

    private BitDataGrid<ProductDto>? dataGrid;


    [AutoInject] IProductController productController = default!;


    private async Task<BitDataGridReadResult<ProductDto>> LoadProducts(BitDataGridReadRequest req)
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var query = new ODataQuery
            {
                Top = req.Take, // null when exporting (CSV/Excel) so the server returns every matching row.
                Skip = req.Skip,
                OrderBy = req.Sorts.Count > 0
                    ? string.Join(", ", req.Sorts.Select(s => $"{s.ColumnId} {(s.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                    : $"{nameof(ProductDto.Name)} asc"
            };

            var filter = string.Join(" and ", req.Filters
                .Where(f => string.IsNullOrEmpty(f.Value?.ToString()) is false)
                .Select(f => $"contains(tolower({f.ColumnId}),'{f.Value!.ToString()!.ToLower().Replace("'", "''")}')"));
            if (string.IsNullOrEmpty(filter) is false)
            {
                query.Filter = filter;
            }

            var queriedRequest = productController.WithQuery(query.ToString());
            var data = await (string.IsNullOrWhiteSpace(searchQuery)
                        ? queriedRequest.GetProducts(req.CancellationToken)
                        : queriedRequest.SearchProducts(searchQuery, req.CancellationToken));

            return new BitDataGridReadResult<ProductDto>(data!.Items!, (int)data!.TotalCount);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return new BitDataGridReadResult<ProductDto>([], 0);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshAsync();
    }

    private async Task CreateProduct()
    {
        NavigationManager.NavigateTo(PageUrls.AddOrEditProduct);
    }

    private async Task DeleteProduct()
    {
        if (deletingProduct is null) return;

        try
        {
            await productController.Delete(deletingProduct.Id, deletingProduct.Version, CurrentCancellationToken);

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

    //#if (brouter == true)
    protected override async ValueTask OnActivated(BrouterRouteActivation activation)
    {
        if (activation.IsFirstActivation is false)
        {
            await RefreshData();
        }
        await base.OnActivated(activation);
    }
    //#endif
}
