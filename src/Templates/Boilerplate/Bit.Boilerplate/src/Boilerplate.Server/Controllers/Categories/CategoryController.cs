using Boilerplate.Client.Core.Controllers.Categories;
using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]"), ApiController]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    [HttpGet, EnableQuery]
    public IQueryable<CategoryDto> Get()
    {
        return DbContext.Categories
            .Project();
    }

    [HttpGet]
    public async Task<PagedResult<CategoryDto>> GetCategories(ODataQueryOptions<CategoryDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

        var totalCount = await query.LongCountAsync(cancellationToken);

        if (odataQuery.Skip is not null)
            query = query.Skip(odataQuery.Skip.Value);

        if (odataQuery.Top is not null)
            query = query.Take(odataQuery.Top.Value);

        return new PagedResult<CategoryDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<CategoryDto> Get(int id, CancellationToken cancellationToken)
    {
        var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (dto is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

        return dto;
    }

    [HttpPost]
    public async Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken)
    {
        var entityToAdd = dto.Map();

        await DbContext.Categories.AddAsync(entityToAdd, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToAdd.Map();
    }

    [HttpPut]
    public async Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.Categories.FirstOrDefaultAsync(t => t.Id == dto.Id, cancellationToken);

        if (entityToUpdate is null)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}")]
    public async Task Delete(int id, CancellationToken cancellationToken)
    {
        if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            throw new BadRequestException(Localizer[nameof(AppStrings.CategoryNotEmpty)]);
        }

        DbContext.Categories.Remove(new() { Id = id });

        var affectedRows = await DbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows < 1)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);
    }
}

