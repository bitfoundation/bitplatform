using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    protected override string? Title => string.Empty;
    protected override string? Subtitle => string.Empty;


    [Parameter] public Guid Id { get; set; }


    [AutoInject] private IProductViewController productViewController = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }


    private ProductDto? product;
    private List<ProductDto>? similarProducts;
    private List<ProductDto>? siblingProducts;
    private bool isLoadingSimilarProducts;
    private bool isLoadingSiblingProducts;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadProduct();

        if (InPrerenderSession is false)
        {
            LoadOtherProducts();
        }
    }

    private async Task LoadProduct()
    {
        product = await productViewController.Get(Id, CurrentCancellationToken);
    }

    private void LoadOtherProducts()
    {
        LoadSimilarProducts().ContinueWith(_ => InvokeAsync(StateHasChanged));
        LoadSiblingProducts().ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    private async Task LoadSimilarProducts()
    {
        if (product is null) return;

        try
        {
            isLoadingSimilarProducts = true;
            similarProducts = await productViewController.GetSimilar(product.Id, CurrentCancellationToken);
        }
        finally
        {
            isLoadingSimilarProducts = false;
        }
    }

    private async Task LoadSiblingProducts()
    {
        if (product is null || product.CategoryId.HasValue is false) return;

        try
        {
            isLoadingSiblingProducts = true;
            siblingProducts = await productViewController.GetSiblings(product.CategoryId.Value, CurrentCancellationToken);
        }
        finally
        {
            isLoadingSiblingProducts = false;
        }
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
}
