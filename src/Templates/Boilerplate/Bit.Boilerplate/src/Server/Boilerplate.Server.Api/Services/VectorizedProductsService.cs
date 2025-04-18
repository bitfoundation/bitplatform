using Microsoft.Extensions.VectorData;
using Boilerplate.Server.Api.Models.Products;
using Microsoft.SemanticKernel.Connectors.InMemory;

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

    private async Task<IVectorStoreRecordCollection<Guid, VectorizedProduct>> GetStore(CancellationToken cancellationToken)
    {
        var vectorStore = new InMemoryVectorStore();
        var productsStore = vectorStore.GetCollection<Guid, VectorizedProduct>("products");
        await productsStore.CreateCollectionIfNotExistsAsync(cancellationToken);
        return productsStore;
    }
}

public class VectorizedProduct
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float>? Embedding { get; set; }
}
