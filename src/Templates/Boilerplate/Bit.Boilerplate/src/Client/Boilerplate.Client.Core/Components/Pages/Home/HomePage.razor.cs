//+:cnd:noEmit
using Boilerplate.Shared.Features.Statistics;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class HomePage
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }


    //#if(module != "Sales")
    private GitHubStats? gitHubStats;
    private NugetStatsDto? nugetStats;
    private bool isLoadingNuget = true;
    private bool isLoadingGitHub = true;


    [AutoInject] private IStatisticsController statisticsController = default!;
    

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // If required, you should typically manage the authorization header for external APIs in **AuthDelegatingHandler.cs**,
        // and error handling in **ExceptionDelegatingHandler.cs**.  

        // These external API calls are provided as sample references for anonymous API usage in anonymous pages,
        // and comprehensive exception handling is not intended for these examples.  

        // However, the logic in other HTTP message handlers, such as **LoggingDelegatingHandler** and **RetryDelegatingHandler**,
        // effectively cover all requests regardless of their destination.

        await Task.WhenAll(LoadNuget(), LoadGitHub());
    }
    

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
    private async Task HandleOnSearchBoxClick()
    {
        PubSubService.Publish(ClientAppMessages.SEARCH_PRODUCTS);
    }
    //#endif
}
