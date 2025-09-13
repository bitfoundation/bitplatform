//+:cnd:noEmit
//#if (database == "PostgreSQL")
using Pgvector.EntityFrameworkCore;
//#endif
using Boilerplate.Server.Api.Models.Products;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// Approaches to implement text search:
/// 1- Simple string matching (e.g., `Contains` method).
/// 2- Full-text search using database capabilities (e.g., PostgreSQL's full-text search).
/// 3- Vector-based search using embeddings (e.g., using OpenAI's embeddings).
/// This service implements vector-based search using embeddings that has the following advantages:
///     - More accurate search results based on semantic meaning rather than just similarity matching.
///     - Multi-language support, as embeddings can capture the meaning of words across different languages.
/// And has the following disadvantages:
///     - Requires additional processing to generate embeddings for the text.
///     - Require more storage space for embeddings compared to simple text search.
/// The simple full-text search would be enough for product search case, but we have implemented the vector-based search to demonstrate how to use embeddings in the project.
/// </summary>
public partial class ProductEmbeddingService
{
    private const float SIMILARITY_THRESHOLD = 0.85f;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IWebHostEnvironment env = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    public async Task<IQueryable<Product>> GetProductsBySearchQuery(string searchQuery, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL" && database != "SqlServer")
        // The RAG has been implemented for PostgreSQL / SQL Server only. Check out https://github.com/bitfoundation/bitplatform/blob/develop/src/Templates/Boilerplate/Bit.Boilerplate/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs
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
        var value = new Microsoft.Data.SqlTypes.SqlVector<float>(embeddedUserQuery.Value);
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        return dbContext.Products
        //#if (database == "PostgreSQL")
            .Where(p => p.Embedding!.CosineDistance(value!) < SIMILARITY_THRESHOLD).OrderBy(p => p.Embedding!.CosineDistance(value!));
        //#elif (database == "SqlServer")
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
            .Where(p => p.Embedding.HasValue && EF.Functions.VectorDistance("cosine", p.Embedding.Value, value) < SIMILARITY_THRESHOLD).OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!.Value, value!));
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        //#endif
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL" && database != "SqlServer")
        return; // The RAG has been implemented for PostgreSQL / SQL Server only.
        //#else
        await dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        // TODO: Needs to be improved.
        var embedding = await EmbedText($@"
Name: **{product.Name}**
Manufacture: **{product.Category!.Name}**
Description: {product.DescriptionText}
Appearance: {product.PrimaryImageAltText}", cancellationToken);

        if (embedding.HasValue)
        {
            product.Embedding = new(embedding.Value);
        }
        //#endif
    }

    private async Task<ReadOnlyMemory<float>?> EmbedText(string input, CancellationToken cancellationToken)
    {
        //#if (database != "PostgreSQL" && database != "SqlServer")
        return null; // The RAG has been implemented for PostgreSQL / SQL Server only.
        //#else
        if (AppDbContext.IsEmbeddingEnabled is false)
            return null;
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator is null)
            return env.IsDevelopment() ? null : throw new InvalidOperationException("Embedding generator is not registered.");

        input = $@"
Name: **{input}**
Manufacture: **{input}**
Description: {input}
Appearance: {input}";


        var embedding = await embeddingGenerator.GenerateVectorAsync(input, options: new() { }, cancellationToken);
        return embedding.ToArray();
        //#endif
    }
}
