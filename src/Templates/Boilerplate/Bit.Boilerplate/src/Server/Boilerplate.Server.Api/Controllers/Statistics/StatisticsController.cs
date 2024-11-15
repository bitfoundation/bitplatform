using Boilerplate.Shared.Controllers.Statistics;
using Boilerplate.Shared.Dtos.Home;

namespace Boilerplate.Server.Api.Controllers.Statistics;

[ApiController, Route("api/[controller]/[action]")]
public partial class StatisticsController : AppControllerBase, IStatisticsController
{
    [AutoInject] private HttpClient httpClient = default!;

    [HttpGet("{packageId}")]
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        var stats = await httpClient.GetFromJsonAsync<NugetStatsDto>($"https://api-v2v3search-0.nuget.org/query?q=packageid:{packageId}", cancellationToken)
                        ?? throw new ResourceNotFoundException(packageId);
        return stats;
    }
}
