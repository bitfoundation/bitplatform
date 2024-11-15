using Boilerplate.Shared.Controllers.Statistics;
using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;

    [CascadingParameter] private BitDir? currentDir { get; set; }

    [AutoInject] private IStatisticsController statisticsController = default!;

    private bool isLoading = true;
    private GitHubStats? gitHubStats;
    private NugetStatsDto? nugetStats;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        try
        {
            (nugetStats, gitHubStats) = await (
                statisticsController.GetNugetStats(packageId: "Bit.BlazorUI", CurrentCancellationToken),
                statisticsController.GetGitHubStats(CurrentCancellationToken));
        }
        finally
        {
            isLoading = false;
        }
    }
}
