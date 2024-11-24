using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Shared.Controllers.Categories;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface ICategoryController : IAppController
{
    [HttpGet("{id}")]
    Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken);

    [HttpPost]
    Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}/{concurrencyStamp}")]
    Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken);

    [HttpGet]
    Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<CategoryDto>> Get(CancellationToken cancellationToken) => default!;
}
