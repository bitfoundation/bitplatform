using System.Text.Json.Nodes;
using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class ProductPage
{
    /// <summary>
    /// <inheritdoc cref="ProductDto.ShortId"/>
    /// </summary>
    [Parameter] public int Id { get; set; }


    [AutoInject] private SignInModalService signInModalService = default!;
    [AutoInject] private IProductViewController productViewController = default!;


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
        catch (ResourceNotFoundException)
        {
            NavigationManager.NotFound();
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

    /// <summary>
    /// An og:image may not be empty - several unfurlers draw a broken image rather than falling back - so a product
    /// with no picture shares the placeholder. It is a static asset of the web app, hence the page's own base url.
    /// </summary>
    private string ShareImageUrl => GetProductImageUrl(product)
        ?? new Uri(new Uri(NavigationManager.BaseUri), ProductImage.PlaceholderSrc).ToString();

    /// <summary>
    /// The schema.org Product of this page, as JSON-LD.
    /// <para>
    /// <see cref="JsonObject"/> rather than a serialized anonymous type: the client heads are published trimmed (and
    /// AOT compiled on some), where an anonymous type has no metadata left and would emit <c>{}</c> at runtime only.
    /// The default encoder escapes angle brackets, so no value can close the script element.
    /// </para>
    /// </summary>
    private string BuildProductJsonLd()
    {
        var schema = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = product!.Name,
            ["sku"] = product.ShortId.ToString(CultureInfo.InvariantCulture),
            ["image"] = ShareImageUrl,
            ["offers"] = new JsonObject
            {
                ["@type"] = "Offer",
                ["url"] = CanonicalUrl,
                ["price"] = product.Price,
                ["priceCurrency"] = product.CurrencyIso ?? "USD", // ProductDto's own documented fallback.
                ["availability"] = "https://schema.org/InStock"
            }
        };

        // Absent rather than null: a null is reported as an invalid property, a missing one is fine.
        if (string.IsNullOrWhiteSpace(product.DescriptionText) is false)
        {
            schema["description"] = product.DescriptionText;
        }

        if (string.IsNullOrWhiteSpace(product.CategoryName) is false)
        {
            schema["category"] = product.CategoryName;
        }

        return schema.ToJsonString();
    }
}
