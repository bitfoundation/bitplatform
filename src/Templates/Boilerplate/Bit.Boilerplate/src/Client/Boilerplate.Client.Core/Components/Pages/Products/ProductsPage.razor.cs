//-:cnd:noEmit
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages.Products;

[Authorize]
public partial class ProductsPage
{
    [AutoInject] IProductController productController = default!;

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
                productController.QueryString.AddRange(new ()
                {
                    { "$top", req.Count ?? 10 },
                    { "$skip", req.StartIndex }
                });

                if (string.IsNullOrEmpty(productNameFilter) is false)
                {
                    productController.QueryString.Add("$filter", $"contains(Name,'{productNameFilter}')");
                }

                if (req.GetSortByProperties().Any())
                {
                    productController.QueryString.Add("$orderby", string.Join(", ", req.GetSortByProperties().Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}")));
                }

                var data = await productController.GetProducts(CurrentCancellationToken);

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
            await productController.Delete(product.Id, CurrentCancellationToken);

            await RefreshData();
        }
    }
}




