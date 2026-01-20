using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsSection
{
    [AutoInject] private IProductViewController productViewController = default!;


    private async ValueTask<IEnumerable<ProductDto>> LoadProducts(BitInfiniteScrollingItemsProviderRequest request)
    {
        try
        {
            return await productViewController
                            .WithQuery(new ODataQuery
                            {
                                Top = 10,
                                Skip = request.Skip,
                                OrderBy = nameof(ProductDto.Name)
                            })
                            .Get(request.CancellationToken);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return [];
        }
    }

    private string? GetProductImageUrl(ProductDto product) => product.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
