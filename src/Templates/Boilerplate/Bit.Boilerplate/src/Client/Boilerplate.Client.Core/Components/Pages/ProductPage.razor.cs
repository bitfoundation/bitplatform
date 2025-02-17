using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    protected override string? Title => string.Empty;
    protected override string? Subtitle => string.Empty;


    /// <summary>
    /// <inheritdoc cref="ProductDto.ShortId"/>
    /// </summary>
    [Parameter] public int Id { get; set; }
    [Parameter] public string? Name { get; set; }


    [AutoInject] private IProductViewController productViewController = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }


    private ProductDto? product;
    private List<ProductDto>? similarProducts;
    private List<ProductDto>? siblingProducts;
    private bool isLoadingProduct = true;
    private bool isLoadingSimilarProducts = true;
    private bool isLoadingSiblingProducts = true;


    protected override async Task OnInitAsync()
    {
        await Task.WhenAll(LoadProduct(), LoadSimilarProducts(), LoadSiblingProducts());

        await base.OnInitAsync();
    }

    private async Task LoadProduct()
    {
        try
        {
            product = await productViewController.Get(Id, CurrentCancellationToken);
        }
        finally
        {
            isLoadingProduct = false;
            StateHasChanged();
        }
    }

    private async Task LoadSimilarProducts()
    {
        try
        {
            similarProducts = await productViewController
                .WithQuery(new ODataQuery { Top = 10 })
                .GetSimilar(Id, CurrentCancellationToken);
        }
        finally
        {
            isLoadingSimilarProducts = false;
            StateHasChanged();
        }
    }

    private async Task LoadSiblingProducts()
    {
        try
        {
            siblingProducts = await productViewController
                .WithQuery(new ODataQuery { Top = 10 })
                .GetSiblings(Id, CurrentCancellationToken);
        }
        finally
        {
            isLoadingSiblingProducts = false;
            StateHasChanged();
        }
    }


    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
}
