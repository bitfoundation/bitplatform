using Boilerplate.Shared.Dtos.Dashboard;

namespace Boilerplate.Client.Core.Controllers.Identity;

[RoutePrefix("api/[controller]/[action]/")]
public interface IDashboardController : IAppController
{
    [HttpGet]
    Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken = default);

    [HttpGet]
    Task<IAsyncEnumerable<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken = default);

    [HttpGet]
    Task<IAsyncEnumerable<ProductSaleStatResponseDto>> GetProductsSalesStats(CancellationToken cancellationToken = default);

    [HttpGet]
    Task<ProductPercentagePerCategoryResponseDto[]> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken = default);
}
