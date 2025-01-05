using Boilerplate.Server.Api.Services;
using Boilerplate.Shared.Dtos.Statistics;
using Boilerplate.Shared.Controllers.Statistics;

namespace Boilerplate.Server.Api.Controllers.Statistics;

[ApiController, Route("api/[controller]/[action]")]
public partial class StatisticsController : AppControllerBase, IStatisticsController
{
    [AutoInject] private NugetStatisticsHttpClient nugetHttpClient = default!;

    [AllowAnonymous]
    [HttpGet("{packageId}")]
    [ResponseCache(Duration = 1 * 24 * 3600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "*" })]
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
    }
}
