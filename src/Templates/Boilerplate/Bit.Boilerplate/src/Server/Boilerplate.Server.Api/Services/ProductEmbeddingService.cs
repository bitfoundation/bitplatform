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
/// 4- Hybrid approach combining full-text search and vector-based search.
/// The vector-based search is overkill for products search, but we implemented it here so you can see how to implement it in case you need it for other scenarios.
/// </summary>
public partial class ProductEmbeddingService
{
    private const float DISTANCE_THRESHOLD = 0.65f;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = default!;

    public async Task<IQueryable<Product>> GetProductsBySearchQuery(string searchQuery, CancellationToken cancellationToken)
    {
        if (AppDbContext.IsEmbeddingEnabled is false)
            throw new InvalidOperationException("Embeddings are not enabled. Please enable them to use this feature.");

        // It would be a good idea to try finding products using full-text search first, and if not enough results are found, then use the vector-based search.

        var embeddedSearchQuery = await embeddingGenerator.GenerateAsync(searchQuery, cancellationToken: cancellationToken);

        //#if (database == "PostgreSQL")
        var value = new Pgvector.Vector(embeddedSearchQuery.Vector);
        //#else
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
        var value = new Microsoft.Data.SqlTypes.SqlVector<float>(embeddedSearchQuery.Vector);
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
        return dbContext.Products
        //#if (database == "PostgreSQL")
            .Where(p => p.Embedding!.CosineDistance(value!) < DISTANCE_THRESHOLD).OrderBy(p => p.Embedding!.CosineDistance(value!));
        //#elif (database == "SqlServer")
        //#if (IsInsideProjectTemplate == true)
        /*
        //#endif
            .Where(p => p.Embedding.HasValue && EF.Functions.VectorDistance("cosine", p.Embedding.Value, value) < DISTANCE_THRESHOLD).OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!.Value, value!));
        //#if (IsInsideProjectTemplate == true)
        */
        //#endif
        //#endif
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        if (AppDbContext.IsEmbeddingEnabled is false)
            throw new InvalidOperationException("Embeddings are not enabled. Please enable them to use this feature.");

        List<(string text, float weight)> inputs = [];

        await dbContext.Entry(product)
            .Reference(p => p.Category)
            .LoadAsync(cancellationToken);

        inputs.Add(($"Id: {product.ShortId}", 0.9f));
        inputs.Add(($"Name: {product.Name}", 0.9f));
        if (string.IsNullOrEmpty(product.DescriptionText) is false)
        {
            inputs.Add((product.DescriptionText, 0.7f));
        }
        if (string.IsNullOrEmpty(product.PrimaryImageAltText) is false)
        {
            inputs.Add((product.PrimaryImageAltText, 0.5f));
        }
        inputs.Add((product.Category!.Name!, 0.9f));

        var texts = inputs.Select(i => i.text).ToArray();

        var embeddingsResponse = await embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);

        var vectors = embeddingsResponse.Select(e => e.Vector.ToArray()).ToArray();
        var weights = inputs.Select(t => t.weight).ToArray();

        if (vectors.Any(v => v.Length != vectors[0].Length))
        {
            throw new InvalidOperationException("All embedding vectors must have the same length.");
        }

        var embedding = new float[vectors[0].Length];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = 0f;
            for (int j = 0; j < vectors.Length; j++)
            {
                embedding[i] += weights[j] * vectors[j][i];
            }
        }

        product.Embedding = new(embedding);
    }
}
