using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsSection
{
    [AutoInject] private IProductViewController productViewController = default!;


    private async ValueTask<IEnumerable<ProductDto>> LoadProducts(BitInfiniteScrollingItemsProviderRequest request)
    {
        try
        {
            return await productViewController
                            .WithQueryString(new ODataQuery { Top = 10, Skip = request.Skip })
                            .Get(request.CancellationToken);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return [];
        }
    }

    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
}
