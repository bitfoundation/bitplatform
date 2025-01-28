using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers.Products;

[Route("api/[controller]/[action]/")]
public interface IProductViewController : IAppController
{
    [HttpGet]
    Task<List<ProductDto>> GetHomeCarouselProducts(CancellationToken cancellationToken) => default!;

    [HttpGet("{skip}/{take}")]
    Task<List<ProductDto>> GetHomeProducts(int skip, int take, CancellationToken cancellationToken);

    [HttpGet("{id}")]
    Task<ProductDto> Get(Guid id, CancellationToken cancellationToken) => default!;

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSimilar(Guid id, CancellationToken cancellationToken) => default!;

    [HttpGet("{id}")]
    Task<List<ProductDto>> GetSiblings(Guid id, CancellationToken cancellationToken) => default!;
}
