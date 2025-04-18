//+:cnd:noEmit
//#if (database != "PostgreSQL")
using Pgvector.EntityFrameworkCore;
//#endif
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// This class stores vectorized products and provides methods to query/manage them.
/// </summary>
public partial class ProductsVectorService
{
    [AutoInject] private AppDbContext dbContext = default!;

    public async Task<IQueryable<Product>> GetProductsByUserNeedsQuery(string userNeedsQuery, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL")
        // The RAG has been implemented for PostgreSQL only. Checkout https://github.com/bitfoundation/bitplatform/blob/develop/src/Templates/Boilerplate/Bit.Boilerplate/src/Server/Boilerplate.Server.Api/Services/ProductsVectorService.cs
        return dbContext.Products.Take(10);
        //#else
        var embeddedUserQuery = await EmbedText(userNeedsQuery, cancellationToken);
        return dbContext.Products
            .OrderBy(p => p.Embedding!.CosineDistance(embeddedUserQuery))
            .Take(5);
        //#endif
    }

    private async Task<float[]> EmbedText(string input, CancellationToken cancellationToken)
    {
        return null!;
    }
}
