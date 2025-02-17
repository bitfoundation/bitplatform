using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Shared.Controllers.Products;

[Route("api/[controller]/[action]/")]
public interface IProductViewController : IAppController
{
    [HttpGet]
    Task<List<ProductDto>> Get(CancellationToken cancellationToken) => default!;

    [HttpGet("{number}")]
    Task<ProductDto> Get(int number, CancellationToken cancellationToken);

    [HttpGet("{number}")]
    Task<List<ProductDto>> GetSimilar(int number, CancellationToken cancellationToken) => default!;

    [HttpGet("{number}")]
    Task<List<ProductDto>> GetSiblings(int number, CancellationToken cancellationToken) => default!;
}
