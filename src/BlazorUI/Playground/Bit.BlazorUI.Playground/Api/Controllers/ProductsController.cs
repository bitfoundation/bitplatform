using Bit.BlazorUI.Playground.Shared.Dtos;
using Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace Bit.BlazorUI.Playground.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ProductsController : ControllerBase
{
    private static readonly Random _random = new Random();

    private static readonly ProductDto[] _products = Enumerable.Range(1, 500_000)
        .Select(i => new ProductDto { Id = i, Name = Guid.NewGuid().ToString("N"), Price = _random.Next(1, 100) })
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
}
