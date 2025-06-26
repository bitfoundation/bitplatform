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
        //#if (database != "PostgreSQL" && database != "SqlServer")
        // The RAG has been implemented for PostgreSQL only. Check out https://github.com/bitfoundation/bitplatform/blob/develop/src/Templates/Boilerplate/Bit.Boilerplate/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs
        return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        //#else
        var embeddedUserQuery = await EmbedText(searchQuery, cancellationToken);
        if (embeddedUserQuery is null)
            return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        //#if (database == "PostgreSQL")
        var value = new Pgvector.Vector(embeddedUserQuery.Value);
        //#else
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        var value = embeddedUserQuery.Value.ToArray();
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        return dbContext.Products
        //#if (database == "PostgreSQL")
            .Where(p => p.Embedding!.CosineDistance(value!) < 0.85f).OrderBy(p => p.Embedding!.CosineDistance(value!));
        //#elif (database == "SqlServer")
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
            .Where(p => p.Embedding != null && EF.Functions.VectorDistance("cosine", p.Embedding, value!) < 0.85f).OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!, value!));
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        //#endif
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL" && database != "SqlServer")
        return;
        //#else
        await dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        // TODO: Needs to be improved.
        var embedding = await EmbedText($"Name: {product.Name}, Manufacture: {product.Category!.Name}, Description: {product.DescriptionText}, Price: {product.Price}", cancellationToken);

        if (embedding.HasValue)
        {
            //#if (database == "PostgreSQL")
            product.Embedding = new(embedding.Value);
            //#elif (database == "SqlServer")
            //#if (IsInsideProjectTemplate == true)
            /*
            //#endif
            product.Embedding = embedding.Value.ToArray();
            //#if (IsInsideProjectTemplate == true)
            */
            //#endif
            //#endif
        }
        //#endif
    }

    private async Task<ReadOnlyMemory<float>?> EmbedText(string input, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL" && database != "SqlServer")
        return null;
        //#else
        if (AppDbContext.IsEmbeddingEnabled is false)
            return null;
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator is null)
            return env.IsDevelopment() ? null : throw new InvalidOperationException("Embedding generator is not registered.");
        var embedding = await embeddingGenerator.GenerateVectorAsync(input, options: new() { }, cancellationToken);
        return embedding.ToArray();
        //#endif
    }
}
