﻿using AdminPanelTemplate.Api.Extensions;
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

    [HttpPost("GetPagedProducts")]
    public async Task<PagedResultDto<ProductDto>> GetPagedProductsAsync(PagedInputDto input, CancellationToken cancellationToken)
    {
        var query = Get(cancellationToken);

        var total = query.Count();

        var filteredQuery = query
                            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), _ => _.Name!.Contains(input.Filter));

        var orderedQuery = (input.SortBy, input.SortAscending) switch
        {
            (nameof(Product.Name), true) => filteredQuery.OrderBy(c => c.Name),
            (nameof(Product.Name), false) => filteredQuery.OrderByDescending(c => c.Name),
            (nameof(Product.Price), true) => filteredQuery.OrderBy(c => c.Price),
            (nameof(Product.Price), false) => filteredQuery.OrderByDescending(c => c.Price),
            _ => filteredQuery.OrderBy(c => c.Id)
        };

        var pageResult = await orderedQuery
                         .PageBy(input.Skip, input.MaxResultCount)
                         .ToListAsync();

        return new PagedResultDto<ProductDto>(pageResult, total);
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
        var productToUpdate = await _dbContext.Products.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (productToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.ProductCouldNotBeFound));

        var updatedTodoItem = _mapper.Map(dto, productToUpdate);

        _dbContext.Products.Update(productToUpdate);

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

