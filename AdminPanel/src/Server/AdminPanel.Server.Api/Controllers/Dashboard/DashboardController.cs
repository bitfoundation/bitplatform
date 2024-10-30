using AdminPanel.Shared.Dtos.Dashboard;
using AdminPanel.Shared.Controllers.Dashboard;

namespace AdminPanel.Server.Api.Controllers;

[ApiController, Route("api/[controller]/[action]")]
public partial class DashboardController : AppControllerBase, IDashboardController
{
    [HttpGet]
    public async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken)
    {
        var result = new OverallAnalyticsStatsDataResponseDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        result.TotalProducts = await DbContext.Products.CountAsync(cancellationToken);
        result.Last30DaysProductCount = await DbContext.Products.CountAsync(p => p.CreatedOn > last30DaysDate, cancellationToken);

        result.TotalCategories = await DbContext.Categories.CountAsync(cancellationToken);
        result.CategoriesWithProductCount = await DbContext.Categories.CountAsync(c => c.Products.Count > 0, cancellationToken);

        return result;
    }

    [HttpGet]
    public async Task<IQueryable<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats()
    {
        return DbContext.Categories
                        .Select(c => new ProductsCountPerCategoryResponseDto()
                        {
                            CategoryName = c.Name,
                            CategoryColor = c.Color,
                            ProductCount = c.Products!.Count()
                        });
    }


    [HttpGet]
    public async Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken)
    {
        var productsTotalCount = await DbContext.Products.CountAsync(cancellationToken);

        if (productsTotalCount == 0) return [];

        return await DbContext.Categories
                              .Select(c => new ProductPercentagePerCategoryResponseDto()
                              {
                                  CategoryName = c!.Name,
                                  CategoryColor = c.Color,
                                  ProductPercentage = (float)decimal.Divide(c.Products!.Count(), productsTotalCount) * 100
                              }).ToListAsync(cancellationToken);
    }
}
