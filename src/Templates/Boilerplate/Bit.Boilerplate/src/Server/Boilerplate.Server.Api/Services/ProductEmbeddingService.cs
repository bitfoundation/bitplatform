//+:cnd:noEmit
//#if (database == "PostgreSQL")
using Pgvector.EntityFrameworkCore;
//#endif
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// This class stores vectorized products and provides methods to query/manage them.
/// </summary>
public partial class ProductEmbeddingService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IWebHostEnvironment env = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    public async Task<IQueryable<Product>> GetProductsBySearchQuery(string searchQuery, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL")
        // The RAG has been implemented for PostgreSQL only. Checkout https://github.com/bitfoundation/bitplatform/blob/develop/src/Templates/Boilerplate/Bit.Boilerplate/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs
        return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        //#else
        var embeddedUserQuery = await EmbedText(searchQuery, cancellationToken);
        if (embeddedUserQuery is null)
            return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        var value = new Pgvector.Vector(embeddedUserQuery.Value);
        return dbContext.Products
            .Where(p => p.Embedding!.CosineDistance(value!) < 0.85f);
        //#endif
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL")
        return;
        //#else
        await dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        // TODO: Needs to be improved.
        var embedding = await EmbedText($"Name: {product.Name}, Manufactor: {product.Category!.Name}, Description: {product.DescriptionText}, Price: {product.Price}", cancellationToken);

        if (embedding.HasValue)
        {
            product.Embedding = new(embedding.Value);
        }
        //#endif
    }

    private async Task<ReadOnlyMemory<float>?> EmbedText(string input, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL")
        return null;
        //#else
        if (AppDbContext.EmbeddingIsEnabled is false)
            return null;
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator is null)
            return env.IsDevelopment() ? throw new InvalidOperationException("Embedding generator is not registered.") : null;
        var embedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(input, options: new() { }, cancellationToken);
        return embedding.ToArray();
        //#endif
    }
}
