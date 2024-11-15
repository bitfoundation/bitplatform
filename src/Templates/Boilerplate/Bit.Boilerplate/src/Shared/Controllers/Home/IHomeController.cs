using Boilerplate.Shared.Dtos.Home;

namespace Boilerplate.Shared.Controllers.Home;

[Route("api/[controller]/[action]/")]
public interface IHomeController : IAppController
{
    [HttpGet("{packageId}")]
    Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken);
}
