﻿using Boilerplate.Shared.Controllers.Home;
using Boilerplate.Shared.Dtos.Home;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class HomePage
{
    protected override string? Title => Localizer[nameof(AppStrings.Home)];
    protected override string? Subtitle => string.Empty;


    [CascadingParameter] private BitDir? currentDir { get; set; }


    [AutoInject] private IHomeController homeController = default!;


    private bool isNugetLoading;
    private bool isGitHubLoading;
    private NugetStatsDto? nugetStats;
    private GitHubStats? gitHubStats;
    private string? nugetStatsString;
    private string? gitHubStatsString;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadGitHubStats();
        await LoadNugetStats("Bit.BlazorUI");
    }


    private async Task LoadGitHubStats()
    {
        try
        {
            isGitHubLoading = true;
            gitHubStats = await HttpClient.GetFromJsonAsync<GitHubStats>("https://api.github.com/repos/bitfoundation/bitplatform");
            gitHubStatsString = JsonSerializer.Serialize(gitHubStats, new JsonSerializerOptions { WriteIndented = true });
        }
        finally
        {
            isGitHubLoading = false;
        }
    }

    private async Task LoadNugetStats(string packageId)
    {
        try
        {
            isNugetLoading = true;
            nugetStats = await homeController.GetNugetStats(packageId, CurrentCancellationToken);
            nugetStatsString = JsonSerializer.Serialize(nugetStats, new JsonSerializerOptions { WriteIndented = true });
        }
        finally
        {
            isNugetLoading = false;
        }
    }
}
