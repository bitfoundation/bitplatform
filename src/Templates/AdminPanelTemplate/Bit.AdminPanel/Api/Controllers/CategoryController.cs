using AdminPanel.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;
using AdminPanel.Api.Extensions;

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

    [HttpPost("GetPagedCategories")]
    public async Task<PagedResultDto<CategoryDto>> GetPagedCategoriesAsync(PagedInputDto input, CancellationToken cancellationToken)
    {
        var query = Get(cancellationToken);

        var total = query.Count();

        var filteredQuery = query
                            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), _ => _.Name!.Contains(input.Filter));

        var orderedQuery = (input.SortBy, input.SortAscending) switch
        {
            (nameof(Category.Name), true) => filteredQuery.OrderBy(c => c.Name),
            (nameof(Category.Name), false) => filteredQuery.OrderByDescending(c => c.Name),
            _ => filteredQuery.OrderBy(c => c.Id)
        };

        var pageResult = await orderedQuery
                         .PageBy(input.Skip, input.MaxResultCount)
                         .ToListAsync();

        return new PagedResultDto<CategoryDto>(pageResult, total);
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

        var updatedTodoItem = _mapper.Map(dto, categoryToUpdate);

        _dbContext.Categories.Update(categoryToUpdate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        if(_dbContext.Products.Any(p => p.CategoryId == id))
        {
            throw new BadRequestException(nameof(ErrorStrings.CategoryNotEmpty));
        }
        

        _dbContext.Remove(new Category { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));
    }
}

