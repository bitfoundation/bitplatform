using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Controllers.Identity;

public interface IProductController : IAppControllerBase
{
    [HttpGet]
    Task<ProductDto> Get(int id, CancellationToken cancellationToken = default);

    [HttpPost]
    Task<ProductDto> Create(ProductDto body, CancellationToken cancellationToken = default);

    [HttpPut]
    Task<ProductDto> Update(ProductDto body, CancellationToken cancellationToken = default);

    [HttpDelete]
    Task Delete(int id, CancellationToken cancellationToken = default);

    [HttpGet]
    Task<PagedResult<ProductDto>> GetProducts(CancellationToken cancellationToken = default) => default!;
}
