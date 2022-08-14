using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class DashboardController : AppControllerBase
{
    [HttpGet]
    public async Task<OverallAnalyticsStatsDataDto> GetOverallAnalyticsStatsData()
    {
        var result = new OverallAnalyticsStatsDataDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        result.TotalProducts = await DbContext.Products.CountAsync();
        result.Last30DaysProductCount = await DbContext.Products.CountAsync(p => p.CreatedOn > last30DaysDate);
        result.TotalCategories = await DbContext.Categories.CountAsync();

        return result;
    }

    [HttpGet]
    public async Task<List<ProductsCountPerCategoryDto>> GetProductsCountPerCategotyStats()
    {
        return await DbContext.Categories
            .Select(c => new ProductsCountPerCategoryDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products!.Count()
            }).ToListAsync();
    }

    [HttpGet]
    public async Task<List<ProductSaleStatDto>> GetProductsSalesStats()
    {
        Random rand = new Random();
        return await DbContext.Products.Include(p => p.Category)
             .Select(p => new ProductSaleStatDto()
             {
                 ProductName = p.Name,
                 CategoryColor = p.Category.Color,
                 SaleAmount = rand.Next(1, 10) * p.Price
             }).ToListAsync();
    }


    [HttpGet]
    public async Task<List<ProductPercentagePerCategoryDto>> GetProductsPercentagePerCategoryStats()
    {
        var productsTotalCount = await DbContext.Products.CountAsync();

        if (productsTotalCount == 0)
        {
            return new List<ProductPercentagePerCategoryDto>();
        }

        return await DbContext.Categories
             .Select(c => new ProductPercentagePerCategoryDto()
             {
                 CategoryName = c!.Name,
                 CategoryColor = c.Color,
                 ProductPercentage = (float)decimal.Divide(c.Products!.Count(), productsTotalCount) * 100
             }).ToListAsync();
    }
}
