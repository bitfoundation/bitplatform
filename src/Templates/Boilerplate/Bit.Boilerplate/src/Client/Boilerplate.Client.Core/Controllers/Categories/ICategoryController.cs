using Boilerplate.Shared.Dtos.Categories;

namespace Boilerplate.Client.Core.Controllers.Categories;

public interface ICategoryController : IAppControllerBase
{
    [HttpGet]
    Task<CategoryDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<CategoryDto> Create(CategoryDto body, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<CategoryDto> Update(CategoryDto body, CancellationToken cancellationToken = default);

    [HttpDelete]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken = default) => default!;

    [HttpGet]
    Task<IAsyncEnumerable<CategoryDto>> Get(CancellationToken cancellationToken) => default!;
}
