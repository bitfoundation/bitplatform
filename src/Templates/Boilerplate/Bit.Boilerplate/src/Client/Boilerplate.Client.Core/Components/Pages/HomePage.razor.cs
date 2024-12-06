using Boilerplate.Shared.Controllers.Statistics;
using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;

    [CascadingParameter] private BitDir? currentDir { get; set; }

    [AutoInject] private IStatisticsController statisticsController = default!;

    private bool isLoadingGitHub = true;
    private bool isLoadingNuget = true;
    private GitHubStats? gitHubStats;
    private NugetStatsDto? nugetStats;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        // If required, you should typically manage the authorization header for external APIs in **AuthDelegatingHandler.cs**
        // and handle error extraction from failed responses in **ExceptionDelegatingHandler.cs**.  
        
        // These external API calls are provided as sample references for anonymous API usage in pre-rendering anonymous pages,
        // and comprehensive exception handling is not intended for these examples.  
        
        // However, the logic in other HTTP message handlers, such as **LoggingDelegatingHandler** and **RetryDelegatingHandler**,
        // effectively addresses most scenarios.

        LoadGitHub();
        LoadNuget();
    }

    private void LoadGitHub()
    {
        _ = statisticsController.GetGitHubStats(CurrentCancellationToken)
                                .ContinueWith(async task =>
                                {
                                    try
                                    {
                                        gitHubStats = await task;
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandler.Handle(ex);
                                    }
                                    finally
                                    {
                                        isLoadingGitHub = false;
                                        await InvokeAsync(StateHasChanged);
                                    }
                                });
    }

    private void LoadNuget()
    {
        _ = statisticsController.GetNugetStats(packageId: "Bit.BlazorUI", CurrentCancellationToken)
                                .ContinueWith(async task =>
                                {
                                    try
                                    {
                                        nugetStats = await task;
                                    }
                                    catch (Exception ex)
                                    {
                                        ExceptionHandler.Handle(ex);
                                    }
                                    finally
                                    {
                                        isLoadingNuget = false;
                                        await InvokeAsync(StateHasChanged);
                                    }
                                });
    }
}
