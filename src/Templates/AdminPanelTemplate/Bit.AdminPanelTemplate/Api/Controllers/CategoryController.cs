using AdminPanelTemplate.Api.Models.Categories;
using AdminPanelTemplate.Shared.Dtos.Categories;

namespace AdminPanelTemplate.Api.Controllers;

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

    [HttpPost("GetPaged")]
    public  async Task<PagedResultDto<CategoryDto>> GetPagedAsync(PagedInputDto input)
    {
        var query = Get(input.CancellationToken);

        var total = query.Count();

        var orderedQuery = (input.SortBy,input.SortAscending) switch
        {
            (nameof(Category.Name), true) => query.OrderBy(c => c.Name),
            _ => query.OrderBy(c => c.Id)
        };

        var pageResult =await orderedQuery.Skip(input.Skip).Take(input.Limit).ProjectTo<CategoryDto>(_mapper.ConfigurationProvider, input.CancellationToken).ToListAsync();

        return new PagedResultDto<CategoryDto>(pageResult,total);

    }

    [HttpGet("{id:int}")]
    public async Task<CategoryDto> Get(int id, CancellationToken cancellationToken)
    {
        var category = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (category is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));

        return category;
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
        _dbContext.Remove(new Category { Id = id });

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));
    }
}

