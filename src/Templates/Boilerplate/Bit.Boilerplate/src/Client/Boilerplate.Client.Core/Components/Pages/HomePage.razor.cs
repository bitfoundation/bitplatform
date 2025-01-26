//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Controllers.Statistics;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;


    [CascadingParameter] private BitDir? currentDir { get; set; }


    //#if(module == "Admin")
    [AutoInject] private IStatisticsController statisticsController = default!;
    private bool isLoadingGitHub = true;
    private bool isLoadingNuget = true;
    private GitHubStats? gitHubStats;
    private NugetStatsDto? nugetStats;
    //#endif

    //#if(module == "Sales")
    [AutoInject] private IProductController productController = default!;
    private IEnumerable<ProductDto>? products;
    //#endif


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        //#if(module == "Admin")
        // If required, you should typically manage the authorization header for external APIs in **AuthDelegatingHandler.cs**
        // and handle error extraction from failed responses in **ExceptionDelegatingHandler.cs**.  

        // These external API calls are provided as sample references for anonymous API usage in pre-rendering anonymous pages,
        // and comprehensive exception handling is not intended for these examples.  

        // However, the logic in other HTTP message handlers, such as **LoggingDelegatingHandler** and **RetryDelegatingHandler**,
        // effectively addresses most scenarios.

        await Task.WhenAll(LoadNuget(), LoadGitHub());
        //#endif

        //#if(module == "Sales")
        products = (await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync("api/Product/GetHomeCarouselProducts",
                                                         JsonSerializerOptions.GetTypeInfo<List<ProductDto>>(),
                                                         CurrentCancellationToken)))!;
        //#endif
    }

    //#if(module == "Admin")
    private async Task LoadNuget()
    {
        try
        {
            nugetStats = await statisticsController.GetNugetStats(packageId: "Bit.BlazorUI", CurrentCancellationToken);
        }
        finally
        {
            isLoadingNuget = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadGitHub()
    {
        try
        {
            // GitHub results (2nd Bit Pivot tab) aren't shown by default and aren't critical for SEO,
            // so we can skip it in pre-rendering to save time.
            if (InPrerenderSession is false)
            {
                gitHubStats = await statisticsController.GetGitHubStats(CurrentCancellationToken);
            }
        }
        catch
        {
            // GetGitHubStats method calls the GitHub API directly from the client.
            // We've intentionally ignored proper exception handling to keep this example simple. 
        }
        finally
        {
            isLoadingGitHub = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    //#endif

    //#if(module == "Sales")
    private async ValueTask<IEnumerable<ProductDto>> LoadProducts(BitInfiniteScrollingItemsProviderRequest request)
    {
        return await productController.GetHomeProducts(request.Skip, 10, CurrentCancellationToken);
    }

    private string GetProductImageUrl(ProductDto product)
    {
        return product.ImageFileName is null
            ? "_content/Boilerplate.Client.Core/images/product-placeholder.png"
            : new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProductImage/{product.Id}?v={product.ConcurrencyStamp}").ToString();
    }
    //#endif
}
