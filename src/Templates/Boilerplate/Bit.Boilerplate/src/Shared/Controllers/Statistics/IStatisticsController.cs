using Boilerplate.Shared.Dtos.Statistics;

namespace Boilerplate.Shared.Controllers.Statistics;

[Route("api/[controller]/[action]/")]
public interface IStatisticsController : IAppController
{
    [HttpGet("{packageId}")]
    Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken);
}
