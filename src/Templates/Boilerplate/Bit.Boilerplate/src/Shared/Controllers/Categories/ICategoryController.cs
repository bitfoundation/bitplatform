using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Shared.Controllers.Categories;

[Route("api/[controller]/[action]/")]
public interface ICategoryController : IAppController
{
    [HttpGet("{id}")]
    Task<CategoryDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken = default);

    [HttpDelete("{id}")]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken = default) => default!;

    [HttpGet]
    Task<List<CategoryDto>> Get(CancellationToken cancellationToken) => default!;
}
