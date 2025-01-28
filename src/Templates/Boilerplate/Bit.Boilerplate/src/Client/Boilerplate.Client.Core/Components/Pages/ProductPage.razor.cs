using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

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

        await Task.WhenAll(LoadProduct(), LoadSimilarProducts());

        await LoadSiblingProducts();
    }

    private async Task LoadProduct()
    {
        product = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync($"api/ProductView/Get/{Id}",
                                                         JsonSerializerOptions.GetTypeInfo<ProductDto>(),
                                                         CurrentCancellationToken)))!;
    }

    private async Task LoadSimilarProducts()
    {
        similarProducts = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync($"api/ProductView/GetSimilar/{Id}",
                                                                      JsonSerializerOptions.GetTypeInfo<List<ProductDto>>(),
                                                                      CurrentCancellationToken)))!;
    }

    private async Task LoadSiblingProducts()
    {
        siblingProducts = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync($"api/ProductView/GetSiblings/{product?.CategoryId}",
                                                                      JsonSerializerOptions.GetTypeInfo<List<ProductDto>>(),
                                                                      CurrentCancellationToken)))!;
    }


    private string GetProductImageUrl(ProductDto product)
    {
        return product.ImageFileName is null
            ? "_content/Boilerplate.Client.Core/images/product-placeholder.png"
            : new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProductImage/{product.Id}?v={product.ConcurrencyStamp}").ToString();
    }
}
