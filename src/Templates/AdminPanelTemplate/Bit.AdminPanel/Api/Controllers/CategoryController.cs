using AdminPanel.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class CategoryController : ControllerBase
{
    [AutoInject] private AppDbContext _dbContext = default!;

    [AutoInject] private IMapper _mapper = default!;

    [HttpGet, EnableQuery]
    public IQueryable<CategoryDto> Get(CancellationToken cancellationToken)
    {
        return _dbContext.Categories
            .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<CategoryDto> Get(int id, CancellationToken cancellationToken)
    {
        var category = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (category is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));

        return category;
    }

    [HttpGet("[action]")]
    public async Task<PagedResult<CategoryDto>> GetCategories(ODataQueryOptions<CategoryDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(Get(cancellationToken), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        if (odataQuery.Top is not null)
            query = query.Take(odataQuery.Top.Value);

        return new PagedResult<CategoryDto>(await query.ToListAsync(cancellationToken), totalCount);
    }

    [HttpPost]
    public async Task Post(CategoryDto dto, CancellationToken cancellationToken)
    {
        var categoryToAdd = _mapper.Map<Category>(dto);

        await _dbContext.Categories.AddAsync(categoryToAdd, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Put(CategoryDto dto, CancellationToken cancellationToken)
    {
        var categoryToUpdate = await _dbContext.Categories.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (categoryToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));

        var updatedCategory = _mapper.Map(dto, categoryToUpdate);

        _dbContext.Categories.Update(categoryToUpdate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        if (_dbContext.Products.Any(p => p.CategoryId == id))
        {
            throw new BadRequestException(nameof(ErrorStrings.CategoryNotEmpty));
        }

        _dbContext.Remove(new Category { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));
    }
}

