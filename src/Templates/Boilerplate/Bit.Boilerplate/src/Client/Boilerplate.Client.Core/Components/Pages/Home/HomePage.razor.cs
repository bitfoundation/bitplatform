//+:cnd:noEmit
using System.Text.Json.Nodes;
using Boilerplate.Shared.Features.Statistics;

namespace Boilerplate.Client.Core.Components.Pages.Home;

public partial class HomePage
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    /// <summary>
    /// The site itself, as JSON-LD. The home page is the one a crawler treats as the site, and it describes no entity
    /// of its own - so a WebSite and the Organization publishing it, where <c>ProductPage</c> carries a Product.
    /// </summary>
    private string BuildSiteJsonLd()
    {
        // The origin, not this page's url: the same two nodes are the site's identity under every culture prefix.
        var siteUrl = new Uri(NavigationManager.BaseUri).GetLeftPart(UriPartial.Authority);

        var organization = new JsonObject
        {
            ["@type"] = "Organization",
            ["@id"] = $"{siteUrl}/#organization",
            ["name"] = "Boilerplate",
            ["url"] = siteUrl,
            ["logo"] = $"{siteUrl}/images/icons/bit-icon-512.png"
        };

        var webSite = new JsonObject
        {
            ["@type"] = "WebSite",
            ["@id"] = $"{siteUrl}/#website",
            ["name"] = "Boilerplate",
            ["url"] = CanonicalUrl,
            ["publisher"] = new JsonObject { ["@id"] = $"{siteUrl}/#organization" }
        };

        if (string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name) is false)
        {
            webSite["inLanguage"] = CultureInfo.CurrentUICulture.Name;
        }

        return new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new JsonArray(organization, webSite)
        }.ToJsonString();
    }


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
    //#if (module == "Sales" && signalR == true)
    private async Task HandleOnSearchBoxClick()
    {
        PubSubService.Publish(ClientAppMessages.SEARCH_PRODUCTS);
    }
    //#endif
}
