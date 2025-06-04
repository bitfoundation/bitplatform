using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    /// <summary>
    /// <inheritdoc cref="ProductDto.ShortId"/>
    /// </summary>
    [Parameter] public int Id { get; set; }


    [AutoInject] private SignInModalService signInModalService = default!;
    [AutoInject] private IProductViewController productViewController = default!;


    [CascadingParameter(Name = Parameters.CurrentUser)]
    public BitDir? CurrentDir { get; set; }


    private ProductDto? product;
    private List<ProductDto>? similarProducts;
    private List<ProductDto>? siblingProducts;
    private bool isLoadingProduct = true;
    private bool isLoadingSimilarProducts = true;
    private bool isLoadingSiblingProducts = true;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await Task.WhenAll(LoadProduct(), LoadSimilarProducts(), LoadSiblingProducts());
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

    private async Task Buy()
    {
        if ((await AuthenticationStateTask).User.IsAuthenticated() is false && await signInModalService.SignIn() is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.YouNeedToSignIn)]);
            return;
        }

        SnackBarService.Success(Localizer[nameof(AppStrings.PurchaseSuccessful)]);
    }

    private string? GetProductImageUrl(ProductDto? product) => product?.GetPrimaryMediumImageUrl(AbsoluteServerAddress);
}
