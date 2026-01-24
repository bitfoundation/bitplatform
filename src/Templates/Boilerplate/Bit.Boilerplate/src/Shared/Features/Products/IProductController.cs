//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Products;

[Route("api/[controller]/[action]/")]
[AuthorizedApi]
public interface IProductController : IAppController
{
    [HttpGet]
    Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<PagedResponse<ProductDto>> GetProducts(CancellationToken cancellationToken) => default!;

    [HttpGet("{searchQuery}")]
    Task<PagedResponse<ProductDto>> SearchProducts(string searchQuery, CancellationToken cancellationToken) => default!;

    [HttpGet("{id}")]
    Task<ProductDto> Get(Guid id, CancellationToken cancellationToken);

    [HttpPost]
    Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<ProductDto> Update(ProductDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}/{version}")]
    Task Delete(Guid id, long version, CancellationToken cancellationToken);
}
