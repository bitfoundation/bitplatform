using AdminPanel.Shared.Dtos.Categories;
using AdminPanel.Shared.Controllers.Categories;

namespace AdminPanel.Server.Api.Controllers;

[ApiController, Route("api/[controller]/[action]")]
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

        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip!.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top!.Value);

        return new PagedResult<CategoryDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }

    [HttpGet("{id}")]
    public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
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
        var entityToUpdate = dto.Map();

        DbContext.Update(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }

    [HttpDelete("{id}/{concurrencyStamp}")]
    public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
    {
        if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
        {
            throw new BadRequestException(Localizer[nameof(AppStrings.CategoryNotEmpty)]);
        }

        DbContext.Categories.Remove(new() { Id = id, ConcurrencyStamp = Convert.FromBase64String(Uri.UnescapeDataString(concurrencyStamp)) });

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}

