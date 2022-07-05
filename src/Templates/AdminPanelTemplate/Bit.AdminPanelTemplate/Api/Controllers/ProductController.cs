using AdminPanelTemplate.Api.Models.Products;
using AdminPanelTemplate.Shared.Dtos.Products;

namespace AdminPanelTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class ProductController : ControllerBase
{
    [AutoInject] private AppDbContext _dbContext = default!;

    [AutoInject] private IMapper _mapper = default!;

    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (product is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));

        return product;
    }

    [HttpPost]
    public async Task Post(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToAdd = _mapper.Map<Product>(dto);

        await _dbContext.Products.AddAsync(productToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Put(ProductDto dto, CancellationToken cancellationToken)
    {
        var productToUpdate = await _dbContext.Categories.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (productToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));

        var updatedTodoItem = _mapper.Map(dto, productToUpdate);

        _dbContext.Categories.Update(productToUpdate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        _dbContext.Remove(new Product { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));
    }
}

