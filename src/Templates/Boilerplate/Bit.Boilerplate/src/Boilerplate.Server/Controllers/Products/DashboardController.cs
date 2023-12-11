using Boilerplate.Shared.Dtos.Dashboard;
using Boilerplate.Client.Core.Controllers.Product;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
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

        return result;
    }

    [HttpGet]
    public async Task<IAsyncEnumerable<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken)
    {
        return DbContext.Categories
            .Select(c => new ProductsCountPerCategoryResponseDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products!.Count()
            }).AsAsyncEnumerable();
    }

    [HttpGet]
    public async Task<IAsyncEnumerable<ProductSaleStatResponseDto>> GetProductsSalesStats(CancellationToken cancellationToken)
    {
        Random rand = new Random();
        return DbContext.Products.Include(p => p.Category)
             .Select(p => new ProductSaleStatResponseDto()
             {
                 ProductName = p.Name,
                 CategoryColor = p.Category!.Color,
                 SaleAmount = rand.Next(1, 10) * p.Price
             }).AsAsyncEnumerable();
    }


    [HttpGet]
    public async Task<ProductPercentagePerCategoryResponseDto[]> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken)
    {
        var productsTotalCount = await DbContext.Products.CountAsync(cancellationToken);

        if (productsTotalCount == 0)
        {
            return [];
        }

        return await DbContext.Categories
             .Select(c => new ProductPercentagePerCategoryResponseDto()
             {
                 CategoryName = c!.Name,
                 CategoryColor = c.Color,
                 ProductPercentage = (float)decimal.Divide(c.Products!.Count(), productsTotalCount) * 100
             }).ToArrayAsync(cancellationToken);
    }
}
