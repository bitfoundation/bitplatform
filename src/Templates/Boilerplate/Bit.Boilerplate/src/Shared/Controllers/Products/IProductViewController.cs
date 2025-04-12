using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers.Products;

[Route("api/[controller]/[action]/")]
public interface IProductViewController : IAppController
{
    [HttpGet]
    Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;

    [HttpGet("{id}")]
    Task<ProductDto> Get(int id, CancellationToken cancellationToken);

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSimilar(int id, CancellationToken cancellationToken) => default!;

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSiblings(int id, CancellationToken cancellationToken) => default!;
}
