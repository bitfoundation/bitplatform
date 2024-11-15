using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Controllers.Statistics;
using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Server.Api.Controllers.Statistics;

[ApiController, Route("api/[controller]/[action]")]
public partial class StatisticsController : AppControllerBase, IStatisticsController
{
    [AutoInject] private NugetStatisticsHttpClient nugetHttpClient = default!;

    [AllowAnonymous]
    [HttpGet("{packageId}")]
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        var stats = await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
        return stats;
    }
}
