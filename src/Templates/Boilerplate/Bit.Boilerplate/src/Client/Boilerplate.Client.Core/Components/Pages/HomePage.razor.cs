//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Statistics;
using Boilerplate.Shared.Controllers.Statistics;
//#if(module == "Sales")
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;
//#endif
namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;


    [CascadingParameter] private BitDir? currentDir { get; set; }


    //#if(module != "Sales")
    [AutoInject] private IStatisticsController statisticsController = default!;
    private bool isLoadingGitHub = true;
    private bool isLoadingNuget = true;
    private GitHubStats? gitHubStats;
    private NugetStatsDto? nugetStats;
    //#endif

    //#if(module == "Sales")
    [AutoInject] private IProductViewController productViewController = default!;
    private IEnumerable<ProductDto>? carouselProducts;
    //#endif


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        //#if(module == "Sales")
        carouselProducts = await productViewController.GetHomeCarouselProducts(CurrentCancellationToken);
        //#endif

        //#if(module != "Sales")
        // If required, you should typically manage the authorization header for external APIs in **AuthDelegatingHandler.cs**
        // and handle error extraction from failed responses in **ExceptionDelegatingHandler.cs**.  

        // These external API calls are provided as sample references for anonymous API usage in pre-rendering anonymous pages,
        // and comprehensive exception handling is not intended for these examples.  

        // However, the logic in other HTTP message handlers, such as **LoggingDelegatingHandler** and **RetryDelegatingHandler**,
        // effectively addresses most scenarios.

        await Task.WhenAll(LoadNuget(), LoadGitHub());
        //#endif
    }

    //#if(module != "Sales")
    private async Task LoadNuget()
    {
        try
        {
            nugetStats = await statisticsController.GetNugetStats(packageId: "Bit.BlazorUI", CurrentCancellationToken);
        }
        finally
        {
            isLoadingNuget = false;
            StateHasChanged();
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
            StateHasChanged();
        }
    }
    //#endif

    //#if(module == "Sales")
    private async ValueTask<IEnumerable<ProductDto>> LoadProducts(BitInfiniteScrollingItemsProviderRequest request)
    {
        try
        {
            return await productViewController
                .WithQueryString(new ODataQuery { Top = 10, Skip = request.Skip })
                .Get(request.CancellationToken);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return [];
        }
    }

    private string? GetProductImageUrl(ProductDto product) => product.GetProductImageUrl(AbsoluteServerAddress);
    //#endif
}
