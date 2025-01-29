using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Server.Api.Controllers.Products;

[ApiController, Route("api/[controller]/[action]")]
[AllowAnonymous]
public partial class ProductViewController : AppControllerBase, IProductViewController
{
    [HttpGet, EnableQuery]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products.Project();
    }

    [HttpGet]
    public async Task<List<ProductDto>> GetHomeCarouselProducts(CancellationToken cancellationToken)
    {
        return await Get().OrderByDescending(p => p.Name)
                          .Take(10)
                          .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ProductDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }


    // This method needs to be implemented based on the logic required in each business.
    [HttpGet("{id}")]
    public async Task<List<ProductDto>> GetSimilar(Guid id, CancellationToken cancellationToken)
    {
        var similarProducts = (await Get().ToListAsync(cancellationToken))
                              .OrderBy(p => Guid.NewGuid())
                              .Where(p => p.Id != id)
                              .Take(10)
                              .ToList();

        return similarProducts;
    }

    [HttpGet("{id}")]
    public async Task<List<ProductDto>> GetSiblings(Guid id, CancellationToken cancellationToken)
    {
        var siblings = await Get().Where(t => t.CategoryId == id)
                                  .Take(10)
                                  .ToListAsync(cancellationToken);

        return siblings;
    }
}

