namespace Boilerplate.Shared.Features.Dashboard;

[Route("api/v1/[controller]/[action]/"), AuthorizedApi]
public interface IDashboardController : IAppController
{
    [HttpGet]
    Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken);
}
