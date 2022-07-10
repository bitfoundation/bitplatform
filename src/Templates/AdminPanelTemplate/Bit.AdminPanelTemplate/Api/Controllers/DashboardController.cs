using AdminPanelTemplate.Shared.Dtos.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelTemplate.Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public partial class DashboardController : ControllerBase
{
    [AutoInject] private AppDbContext _dbContext = default!;

    [AutoInject] private IMapper _mapper = default!;

    [HttpGet("GetOverallAnalyticsStatsData")]
    public async Task<OverallAnalyticsStatsDataDto> GetOverallAnalyticsStatsData()
    {
        //Thread.Sleep(5000);
        var result = new OverallAnalyticsStatsDataDto();

        int curQuarter = (DateTime.Now.Month - 1) / 3 + 1;

        result.TotalProducts = await _dbContext.Products.CountAsync();
        result.SeasonProductCount = await _dbContext.Products.CountAsync(p => (p.CreateDate.Month - 1) / 3 + 1 == curQuarter);
        result.TotalCategories = await _dbContext.Categories.CountAsync();

        return result;

    }


    [HttpGet("GetPproductsCountPerCategotyStats")]
    public async Task<List<ProductsCountPerCategoryDto>> GetPproductsCountPerCategotyStats()
    {
        return await _dbContext.Categories
            .Select(c => new ProductsCountPerCategoryDto()
            {
                CategoryName = c.Name,
                CategoryColor = c.Color,
                ProductCount = c.Products.Count()
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


    [HttpGet("GetProductsPercentagePerCategoryStats")]
    public async Task<List<ProductPercentagePerCategoryDto>> GetProductsPercentagePerCategoryStats()
    {
        var ProductToal= await _dbContext.Products.CountAsync();

        if(ProductToal==0)
        {
            return new List<ProductPercentagePerCategoryDto>();
        }

        //return await _dbContext.Categories
        //    .Select(c => new ProductPercentagePerCategoryDto()
        //    {
        //        CategoryName = c.Name,
        //        CategoryColor = c.Color,
        //        ProductPercentage = ((c.Products.Count())/ProductToal)*100 
        //    }).ToListAsync();

       return await _dbContext.Categories
            .Select(c => new ProductPercentagePerCategoryDto()
            {
                CategoryName = c!.Name,
                CategoryColor = c.Color,
                ProductPercentage =(float)decimal.Divide(c.Products!.Count(), ProductToal) * 100
            }).ToListAsync();

       

    }

}
