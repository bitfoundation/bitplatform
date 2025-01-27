using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;


    [Parameter] public Guid Id { get; set; }


    [AutoInject] private IProductController productController = default!;
    private ProductDto? product;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        
        product = await productController.GetForSales(Id, CurrentCancellationToken);
    }


    private string GetProductImageUrl(ProductDto product)
    {
        return product.ImageFileName is null
            ? "_content/Boilerplate.Client.Core/images/product-placeholder.png"
            : new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProductImage/{product.Id}?v={product.ConcurrencyStamp}").ToString();
    }
}
