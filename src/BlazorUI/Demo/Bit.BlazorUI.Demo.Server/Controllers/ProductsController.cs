namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public partial class ProductsController : AppControllerBase
{
    private static readonly Random _random = new Random();

    private static readonly ProductDto[] _products = Enumerable.Range(1, 500_000)
        .Select(i => new ProductDto { Id = i, Name = $"Product {i}", Price = _random.Next(1, 100) })
        .ToArray();

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = _products.AsQueryable();

        query = (IQueryable<ProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        query = query.Take(odataQuery.Top?.Value ?? 50);

        return new PagedResult<ProductDto>(query.ToArray(), totalCount);
    }

    private static readonly List<CategoryOrProductDto> _categoriesProducts = new();

    static ProductsController()
    {
        foreach (var category in Enumerable.Range(1, 50_000).Select(i => new CategoryOrProductDto
        {
            CategoryId = i,
            Name = $"Category {i.ToString("N0")}"
        }))
        {
            _categoriesProducts.Add(category);

            foreach (var product in Enumerable.Range(1, 10).Select(i => new CategoryOrProductDto
            {
                ProductId = i,
                Name = $"Product {i} at {category.CategoryId?.ToString("N0")}",
                Price = new Random().Next(10, 1000),
                CategoryId = category.CategoryId
            }))
            {
                _categoriesProducts.Add(product);
            }
        }
    }

    [HttpGet]
    public async Task<PagedResult<CategoryOrProductDto>> GetCategoriesAndProducts(ODataQueryOptions<CategoryOrProductDto> odataQuery, CancellationToken cancellationToken)
    {
        //note:if want to fetch data from DB:
        // var query = _dbContext.Categories.Select(cat => new CategoryOrProductDto { CategoryId = cat.Id, Name = cat.Name })
        // .Union(_dbContext.Products.Select(prd => new CategoryOrProductDto { ProductId = prd.Id, Name = prd.Name, Price = prd.Price, CategoryId = prd.CategoryId })
        // .OrderBy(catOrPrd => catOrPrd.CategoryId);

        var query = _categoriesProducts.AsQueryable();

        query = (IQueryable<CategoryOrProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        query = query.Take(odataQuery.Top?.Value ?? 10);

        return new PagedResult<CategoryOrProductDto>(query.ToArray(), totalCount);
    }
}
