using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Server.Api.Controllers.Products;

[ApiController, Route("api/[controller]/[action]")]
[AllowAnonymous]
public partial class ProductViewController : AppControllerBase, IProductViewController
{
    [HttpGet, EnableQuery, AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products.Project();
    }

    [HttpGet("{id}")]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5, UserAgnostic = true)]
    public async Task<ProductDto> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }


    // This method needs to be implemented based on the logic required in each business.
    [EnableQuery, HttpGet("{id}")]
    public IQueryable<ProductDto> GetSimilar(Guid id)
    {
        var similarProducts = Get()
                              .OrderBy(p => EF.Functions.Random())
                              .Where(p => p.Id != id);

        return similarProducts;
    }

    [HttpGet("{id}")]
    public IQueryable<ProductDto> GetSiblings(Guid id)
    {
        var siblings = Get().Where(t => t.CategoryId == id);

        return siblings;
    }
}

