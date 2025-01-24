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
            Public = true,
            MaxAge = TimeSpan.FromDays(1),
            SharedMaxAge = TimeSpan.FromDays(7)
        };

        return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
    }
}
