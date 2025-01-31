using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers.Products;

[Route("api/[controller]/[action]/")]
public interface IProductViewController : IAppController
{
    [HttpGet]
    Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<ProductDto>> GetHomeCarouselProducts(CancellationToken cancellationToken);

    [HttpGet("{id}")]
    Task<ProductDto> Get(Guid id, CancellationToken cancellationToken);

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSimilar(Guid id, CancellationToken cancellationToken);

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSiblings(Guid id, CancellationToken cancellationToken);
}
