using AdminPanel.Shared.Dtos.Dashboard;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class DashboardController : ControllerBase
{
    [AutoInject] private AppDbContext _dbContext = default!;

    [HttpGet("[action]")]
    public async Task<OverallAnalyticsStatsDataDto> GetOverallAnalyticsStatsData()
    {
        var result = new OverallAnalyticsStatsDataDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        result.TotalProducts = await _dbContext.Products.CountAsync();
        result.Last30DaysProductCount = await _dbContext.Products.CountAsync(p => p.CreatedOn > last30DaysDate);
        result.TotalCategories = await _dbContext.Categories.CountAsync();

        return result;
    }

    [HttpGet("[action]")]
    public async Task<List<ProductsCountPerCategoryDto>> GetProductsCountPerCategotyStats()
    {
        return await _dbContext.Categories
            .Select(c => new ProductsCountPerCategoryDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products!.Count()
            }).ToListAsync();
    }

    [HttpGet("GetProductsSalesStats")]
    public async Task<List<ProductSaleStatDto>> GetProductsSalesStats()
    {
        Random rand = new Random();

        var products = await _dbContext.Products.ToListAsync();

        return products.Select(p => new ProductSaleStatDto()
        {
            ProductName = p.Name,
            SaleAmount = rand.Next(1, 10) * p.Price
        }).ToList();
    }


    [HttpGet("[action]")]
    public async Task<List<ProductPercentagePerCategoryDto>> GetProductsPercentagePerCategoryStats()
    {
        var productsTotalCount = await _dbContext.Products.CountAsync();

        if (productsTotalCount == 0)
        {
            return new List<ProductPercentagePerCategoryDto>();
        }

        return await _dbContext.Categories
             .Select(c => new ProductPercentagePerCategoryDto()
             {
                 CategoryName = c!.Name,
                 CategoryColor = c.Color,
                 ProductPercentage = (float)decimal.Divide(c.Products!.Count(), productsTotalCount) * 100
             }).ToListAsync();
    }
}
