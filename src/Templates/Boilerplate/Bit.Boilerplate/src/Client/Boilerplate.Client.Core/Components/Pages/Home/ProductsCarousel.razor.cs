using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsCarousel
{
    [AutoInject] private IProductViewController productViewController = default!;


    private bool isLoading = true;
    private IEnumerable<ProductDto>? carouselProducts;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        try
        {
            carouselProducts = await productViewController
                                        .WithQuery(new ODataQuery
                                        {
                                            Top = 6,
                                            OrderBy = nameof(ProductDto.Name)
                                        })
                                        .Get(CurrentCancellationToken);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
        finally
        {
            isLoading = false;
        }
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
