using AdminPanel.Api.Models.Products;
using AdminPanel.Shared.Dtos.Products;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ProductController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get(CancellationToken cancellationToken)
    {
        return DbContext.Products
            .ProjectTo<ProductDto>(Mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (product is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));

        return product;
    }

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(Get(cancellationToken), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        if (odataQuery.Top is not null)
            query = query.Take(odataQuery.Top.Value);

        return new PagedResult<ProductDto>(await query.ToListAsync(cancellationToken), totalCount);
    }

    [HttpPost]
    public async Task Create(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToAdd = Mapper.Map<Product>(dto);

        await DbContext.Products.AddAsync(productToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Update(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToUpdate = await DbContext.Products.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (productToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));

        var updatedProduct = Mapper.Map(dto, productToUpdate);

        DbContext.Products.Update(productToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        DbContext.Remove(new Product { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));
    }
}

