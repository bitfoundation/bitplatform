using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.OData;

namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public partial class ProductsController : AppControllerBase
{
    private static readonly Random _random = new Random();

    // $top is applied by hand below (after the count), so it is bounded here: without MaxTop a single
    // request could ask for every one of the half a million rows. 1000 is past any window the demos'
    // virtualized lists and grids ask for.
    private static readonly ODataValidationSettings _validationSettings = new()
    {
        MaxTop = 1000,
        AllowedFunctions = AllowedFunctions.AllFunctions & ~AllowedFunctions.MatchesPattern
    };

    private static readonly ProductDto[] _products = Enumerable.Range(1, 500_000)
        .Select(i => new ProductDto { Id = i, Name = $"Product {i}", Price = _random.Next(1, 100) })
        .ToArray();

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        // A query past the limits is the client's mistake: a 400 that says why, not a 500.
        try
        {
            odataQuery.Validate(_validationSettings);
        }
        catch (ODataException exception)
        {
            return BadRequest(exception.Message);
        }

        var query = _products.AsQueryable();

        query = (IQueryable<ProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        query = query.Take(odataQuery.Top?.Value ?? 50);

        return Ok(new PagedResult<ProductDto>(query.ToArray(), totalCount));
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
    public async Task<ActionResult<PagedResult<CategoryOrProductDto>>> GetCategoriesAndProducts(ODataQueryOptions<CategoryOrProductDto> odataQuery, CancellationToken cancellationToken)
    {
        //note:if want to fetch data from DB:
        // var query = _dbContext.Categories.Select(cat => new CategoryOrProductDto { CategoryId = cat.Id, Name = cat.Name })
        // .Union(_dbContext.Products.Select(prd => new CategoryOrProductDto { ProductId = prd.Id, Name = prd.Name, Price = prd.Price, CategoryId = prd.CategoryId })
        // .OrderBy(catOrPrd => catOrPrd.CategoryId);

        try
        {
            odataQuery.Validate(_validationSettings);
        }
        catch (ODataException exception)
        {
            return BadRequest(exception.Message);
        }

        var query = _categoriesProducts.AsQueryable();

        query = (IQueryable<CategoryOrProductDto>)odataQuery.ApplyTo(query, ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = query.Count();

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        query = query.Take(odataQuery.Top?.Value ?? 10);

        return Ok(new PagedResult<CategoryOrProductDto>(query.ToArray(), totalCount));
    }
}
