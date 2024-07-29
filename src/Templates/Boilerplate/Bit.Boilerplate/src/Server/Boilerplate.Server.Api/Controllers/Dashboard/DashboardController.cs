using Boilerplate.Shared.Dtos.Dashboard;
using Boilerplate.Server.Api.Models.Products;
using Boilerplate.Server.Api.Models.Categories;
using Boilerplate.Shared.Controllers.Dashboard;

namespace Boilerplate.Server.Api.Controllers;

[ApiController, Route("api/[controller]/[action]")]
public partial class DashboardController : AppControllerBase, IDashboardController
{
    [HttpGet]
    public async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken)
    {
        var result = new OverallAnalyticsStatsDataResponseDto();

        var last30DaysDate = DateTimeOffset.UtcNow.AddDays(-30);

        result.TotalProducts = await DbContext.Set<Product>().CountAsync(cancellationToken);
        result.Last30DaysProductCount = await DbContext.Set<Product>().CountAsync(p => p.CreatedOn > last30DaysDate, cancellationToken);
        result.TotalCategories = await DbContext.Set<Category>().CountAsync(cancellationToken);

        return result;
    }

    [HttpGet]
    public IQueryable<ProductsCountPerCategoryResponseDto> GetProductsCountPerCategoryStats()
    {
        return DbContext.Set<Category>()
            .Select(c => new ProductsCountPerCategoryResponseDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products!.Count()
            });
    }

    [HttpGet]
    public IQueryable<ProductSaleStatResponseDto> GetProductsSalesStats()
    {
        Random rand = new Random();
        return DbContext.Set<Product>().Include(p => p.Category)
             .Select(p => new ProductSaleStatResponseDto()
             {
                 ProductName = p.Name,
                 CategoryColor = p.Category!.Color,
                 SaleAmount = rand.Next(1, 10) * p.Price
             });
    }


    [HttpGet]
    public async Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken)
    {
        var productsTotalCount = await DbContext.Set<Product>().CountAsync(cancellationToken);

        if (productsTotalCount == 0)
        {
            return [];
        }

        return await DbContext.Set<Category>()
             .Select(c => new ProductPercentagePerCategoryResponseDto()
             {
                 CategoryName = c!.Name,
                 CategoryColor = c.Color,
                 ProductPercentage = (float)decimal.Divide(c.Products!.Count(), productsTotalCount) * 100
             }).ToListAsync(cancellationToken);
    }
}
