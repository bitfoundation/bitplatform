using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.Shared.Controllers.Dashboard;

[Route("api/[controller]/[action]/")]
public interface IDashboardController : IAppController
{
    [HttpGet]
    Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken);
}
