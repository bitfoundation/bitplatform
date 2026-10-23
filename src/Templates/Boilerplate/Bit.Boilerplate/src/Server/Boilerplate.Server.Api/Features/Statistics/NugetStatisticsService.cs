using ZiggyCreatures.Caching.Fusion;
using Boilerplate.Shared.Features.Statistics;

namespace Boilerplate.Server.Api.Features.Statistics;

public partial class NugetStatisticsService
{
    [AutoInject] protected HttpClient httpClient = default!;
    [AutoInject] protected IFusionCache cache = default!;
    [AutoInject] protected JsonSerializerOptions jsonSerializerOptions = default!;

    public virtual async ValueTask<NugetStatsDto> GetPackageStats(string packageId, CancellationToken cancellationToken)
    {
        return await cache.GetOrSetAsync<NugetStatsDto>($"nuget-stats:{packageId}", async (_, token) =>
        {
            var url = $"/query?q=packageid:{Uri.EscapeDataString(packageId)}";

            return await httpClient.GetFromJsonAsync(url, jsonSerializerOptions.GetTypeInfo<NugetStatsDto>(), token)
                        ?? throw new ResourceNotFoundException().WithData("Reason", $"NuGet package '{packageId}' not found.");
        }, options => options.SetDuration(TimeSpan.FromDays(1)), token: cancellationToken);
    }
}
