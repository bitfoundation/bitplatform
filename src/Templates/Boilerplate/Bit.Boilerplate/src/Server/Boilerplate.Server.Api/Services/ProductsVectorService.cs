//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// This class stores vectorized products and provides methods to query/manage them.
/// </summary>
public partial class ProductsVectorService
{
    [AutoInject] private AppDbContext dbContext = default!;

    public IQueryable<Product> GetProductsByUserNeedsQuery(string userNeedsQuery)
    {
        //#if (database != "PostgreSQL")
        // The RAG has been implemented for PostgreSQL only. Checkout 
        return dbContext.Products.Take(10);
        //#else

        //#endif
    }
}
