using Boilerplate.Shared.Dtos.Dashboard;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class DashboardController : AppControllerBase
{
    [HttpGet]
    public async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData()
    {
        var result = new OverallAnalyticsStatsDataResponseDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        result.TotalProducts = await DbContext.Products.CountAsync();
        result.Last30DaysProductCount = await DbContext.Products.CountAsync(p => p.CreatedOn > last30DaysDate);
        result.TotalCategories = await DbContext.Categories.CountAsync();

        return result;
    }

    [HttpGet]
    public async Task<List<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategotyStats()
    {
        return await DbContext.Categories
            .Select(c => new ProductsCountPerCategoryResponseDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products!.Count()
            }).ToListAsync();
    }

    [HttpGet]
    public async Task<List<ProductSaleStatResponseDto>> GetProductsSalesStats()
    {
        Random rand = new Random();
        return await DbContext.Products.Include(p => p.Category)
             .Select(p => new ProductSaleStatResponseDto()
             {
                 ProductName = p.Name,
                 CategoryColor = p.Category!.Color,
                 SaleAmount = rand.Next(1, 10) * p.Price
             }).ToListAsync();
    }


    [HttpGet]
    public async Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats()
    {
        var productsTotalCount = await DbContext.Products.CountAsync();

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
             }).ToListAsync();
    }
}
