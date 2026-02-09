using Boilerplate.Shared.Features.Products;

namespace Boilerplate.Server.Api.Features.Products;

[ApiVersion(1)]
[AllowAnonymous]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class ProductViewController : AppControllerBase, IProductViewController
{
    [HttpGet, EnableQuery, AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public IQueryable<ProductDto> Get()
    {
        return DbContext.Products.Project();
    }

    [HttpGet("{id}")]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5, UserAgnostic = true)]
    public async Task<ProductDto> Get(int id, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.ShortId == id, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }


    // This method needs to be implemented based on the logic required in each business.
    [EnableQuery, HttpGet("{id}"), AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public IQueryable<ProductDto> GetSimilar(int id)
    {
        var similarProducts = Get()
                              .OrderBy(p => EF.Functions.Random())
                              .Where(p => p.ShortId != id);

        return similarProducts;
    }

    [EnableQuery, HttpGet("{id}"), AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public async Task<IQueryable<ProductDto>> GetSiblings(int id, CancellationToken cancellationToken)
    {
        var categoryId = await DbContext.Products
            .Where(p => p.ShortId == id)
            .Select(p => p.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);

        var siblings = Get().Where(t => t.ShortId != id && t.CategoryId == categoryId);

        return siblings;
    }
}

