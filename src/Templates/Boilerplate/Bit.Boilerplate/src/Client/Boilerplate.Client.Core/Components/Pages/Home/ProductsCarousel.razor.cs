using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class ProductsCarousel
{
    [AutoInject] private IProductViewController productViewController = default!;


    private bool loadFailed;
    private bool isLoading = true;
    private IEnumerable<ProductDto>? carouselProducts;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadAsync();
    }

    // A failed request and an empty catalogue are different things, and the carousel must not show one as the other.
    private async Task LoadAsync()
    {
        loadFailed = false;
        isLoading = true;

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
            loadFailed = true;
            ExceptionHandler.Handle(exp);
        }
        finally
        {
            isLoading = false;
        }
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
