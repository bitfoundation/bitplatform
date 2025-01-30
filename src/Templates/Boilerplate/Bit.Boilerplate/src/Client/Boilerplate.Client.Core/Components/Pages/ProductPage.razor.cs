using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    protected override string? Title => string.Empty;
    protected override string? Subtitle => string.Empty;


    [Parameter] public Guid Id { get; set; }


    [AutoInject] private IProductViewController productViewController = default!;


    private ProductDto? product;
    private List<ProductDto>? similarProducts;
    private List<ProductDto>? siblingProducts;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadProduct();

        await Task.WhenAll(LoadSimilarProducts(), LoadSiblingProducts());
    }

    private async Task LoadProduct()
    {
        product = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync($"api/ProductView/Get/{Id}",
                                                        JsonSerializerOptions.GetTypeInfo<ProductDto>(),
                                                        CurrentCancellationToken)))!;
    }

    private async Task LoadSimilarProducts()
    {
        if (product is null) return;

        similarProducts = await productViewController.GetSimilar(product.Id, CurrentCancellationToken);
    }

    private async Task LoadSiblingProducts()
    {
        if (product is null || product.CategoryId.HasValue is false) return;

        siblingProducts = await productViewController.GetSiblings(product.CategoryId.Value, CurrentCancellationToken);
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
}
