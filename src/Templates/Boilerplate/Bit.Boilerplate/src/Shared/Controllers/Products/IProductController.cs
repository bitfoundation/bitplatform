using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers.Product;

[Route("api/[controller]/[action]/")]
public interface IProductController : IAppController
{
    [HttpGet("{id}")]
    Task<ProductDto> Get(Guid id, CancellationToken cancellationToken);

    [HttpPost]
    Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}/{concurrencyStamp}")]
    Task Delete(Guid id, [Base64String] string concurrencyStamp, CancellationToken cancellationToken);

    [HttpGet]
    Task<PagedResult<ProductDto>> GetProducts(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;
}
