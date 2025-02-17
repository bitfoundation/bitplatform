﻿using Boilerplate.Shared.Dtos.Products;
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

    [HttpGet("{number}")]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5, UserAgnostic = true)]
    public async Task<ProductDto> Get(int number, CancellationToken cancellationToken)
    {
        var product = await Get().FirstOrDefaultAsync(t => t.Number == number, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductCouldNotBeFound)]);

        return product;
    }


    // This method needs to be implemented based on the logic required in each business.
    [EnableQuery, HttpGet("{number}"), AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public IQueryable<ProductDto> GetSimilar(int number)
    {
        var similarProducts = Get()
                              .OrderBy(p => EF.Functions.Random())
                              .Where(p => p.Number != number);

        return similarProducts;
    }

    [EnableQuery, HttpGet("{number}"), AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
    public async Task<IQueryable<ProductDto>> GetSiblings(int number, CancellationToken cancellationToken)
    {
        var categoryId = await DbContext.Products
            .Where(p => p.Number == number)
            .Select(p => p.CategoryId)
            .FirstOrDefaultAsync(cancellationToken);

        var siblings = Get().Where(t => t.CategoryId == categoryId);

        return siblings;
    }
}

