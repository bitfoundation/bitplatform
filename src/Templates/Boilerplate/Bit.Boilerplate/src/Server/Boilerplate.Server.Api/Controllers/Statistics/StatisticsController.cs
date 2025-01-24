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
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        Response.GetTypedHeaders().CacheControl = new()
        {
            MaxAge = TimeSpan.FromDays(7),
            Public = true
        };

        return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
    }
}
