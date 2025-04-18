using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// This class stores vectorized products and provides methods to query/manage them.
/// </summary>
public partial class VectorizedProductsService
{
    [AutoInject] private AppDbContext dbContext = default!;

    public async Task DeleteVectorizedProduct(Guid productId)
    {

    }

    public async Task UpsertVectorizedProduct(Product product)
    {

    }

    public async Task<IQueryable<Product>> GetVectorizedProducts(string userNeedsQuery, CancellationToken cancellationToken)
    {
        return dbContext.Products;
    }
}
