using AdminPanel.Api.Models.Categories;
using AdminPanel.Shared.Dtos.Categories;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class CategoryController : AppControllerBase
{
    [HttpGet, EnableQuery]
    public IQueryable<CategoryDto> Get(CancellationToken cancellationToken)
    {
        return DbContext.Categories
            .ProjectTo<CategoryDto>(Mapper.ConfigurationProvider, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<CategoryDto> Get(int id, CancellationToken cancellationToken)
    {
        var category = await Get(cancellationToken).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (category is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));

        return category;
    }

    [HttpGet]
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
    public async Task Create(CategoryDto dto, CancellationToken cancellationToken)
    {
        var categoryToAdd = Mapper.Map<Category>(dto);

        await DbContext.Categories.AddAsync(categoryToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpPut]
    public async Task Update(CategoryDto dto, CancellationToken cancellationToken)
    {
        var categoryToUpdate = await DbContext.Categories.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (categoryToUpdate is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));

        var updatedCategory = Mapper.Map(dto, categoryToUpdate);

        DbContext.Categories.Update(categoryToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        if (DbContext.Products.Any(p => p.CategoryId == id))
        {
            throw new BadRequestException(nameof(ErrorStrings.CategoryNotEmpty));
        }

        DbContext.Remove(new Category { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(nameof(ErrorStrings.CategoryCouldNotBeFound));
    }
}

