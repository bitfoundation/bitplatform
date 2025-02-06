using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsCarousel
{
    private BitCarousel carouselRef = default!;
    private IEnumerable<ProductDto>? carouselProducts;


    [AutoInject] private IProductViewController productViewController = default!;


    protected override async Task OnInitAsync()
    {
        carouselProducts = await productViewController.GetHomeCarouselProducts(CurrentCancellationToken);

        await base.OnInitAsync();
    }

    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
}
