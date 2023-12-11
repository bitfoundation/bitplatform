using Boilerplate.Client.Core.Controllers.Product;
using Boilerplate.Server.Models.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class ProductController : AppControllerBase, IProductController
{
    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products
            .Project();
    }

    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetProducts(ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        if (odataQuery.Top is not null)
            query = query.Take(odataQuery.Top.Value);

        return new PagedResult<ProductDto>(query.AsAsyncEnumerable(), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (product is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }

    [HttpPost]
    public async Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToAdd = dto.Map();

        await DbContext.Products.AddAsync(productToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return productToAdd.Map();
    }

    [HttpPut]
    public async Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToUpdate = await DbContext.Products.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (productToUpdate is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        dto.Patch(productToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return productToUpdate.Map();
    }

    [HttpDelete("{id}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        DbContext.Remove(new Product { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);
    }
}

