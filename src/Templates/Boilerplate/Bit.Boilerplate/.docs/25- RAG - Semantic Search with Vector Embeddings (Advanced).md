# Stage 25: RAG - Semantic Search with Vector Embeddings (Advanced)

Welcome to Stage 25! In this advanced stage, you will learn about the powerful semantic search capabilities using vector embeddings for database queries. This feature enables meaning-based searches that go beyond simple keyword matching.

---

## 1. Different Search Approaches

The project supports multiple approaches to implement text search, each with different capabilities and complexity levels:

### 1.1 Simple String Matching
- **Description**: Basic `Contains()` method for searching text in database fields
- **Example**: `products.Where(p => p.Name.Contains("laptop"))`
- **Pros**: Simple to implement, no additional setup required
- **Cons**: Limited to exact or partial keyword matches, no understanding of meaning or synonyms

### 1.2 Full-Text Search
- **Description**: Database-native full-text search capabilities
- **Examples**: PostgreSQL's full-text search, SQL Server's Full-Text Search
- **Pros**: Better performance than simple string matching, supports stemming and ranking
- **Cons**: Still keyword-based, doesn't understand semantic meaning or cross-language queries

### 1.3 Vector-Based Semantic Search
- **Description**: Using text-embedding models to convert text into numerical vectors that capture semantic meaning
- **Implementation**: The project uses `IEmbeddingGenerator<string, Embedding<float>>` from Microsoft.Extensions.AI
- **Pros**: 
  - Understands meaning, not just keywords
  - Can find semantically similar results even with different wording
  - Supports cross-language searches
- **Cons**: Requires AI model integration, more complex setup, higher computational cost

### 1.4 Hybrid Approach
- **Description**: Combining full-text search with vector-based search for optimal results
- **Implementation**: Try full-text search first, then fall back to vector search if not enough results
- **Pros**: Best of both worlds - speed and semantic understanding
- **Cons**: Most complex to implement

---

## 2. Understanding Vector Embeddings

### 2.1 What Are Embeddings?

**Embeddings** are numerical representations (vectors) of text that capture semantic meaning. Think of them as coordinates in a multi-dimensional space where semantically similar concepts are located close to each other.

**Example**:
```
"laptop computer" → [0.23, -0.45, 0.78, ..., 0.12]  (384 dimensions)
"notebook PC"     → [0.25, -0.43, 0.80, ..., 0.14]  (384 dimensions)
"apple fruit"     → [-0.67, 0.34, -0.21, ..., 0.89] (384 dimensions)
```

Notice that "laptop computer" and "notebook PC" have similar vectors (close in semantic space), while "apple fruit" is far away.

### 2.2 Semantic Search Capability

With vector embeddings, you can perform searches that understand meaning, not just keywords:

**Traditional Keyword Search**:
- Query: "laptop computer"
- Finds: Only products containing the exact words "laptop" or "computer"

**Semantic Vector Search**:
- Query: "laptop computer"
- Finds: 
  - Products with "laptop", "computer", "notebook", "PC", "portable workstation"
  - Even products described with different but related terms
  - Can even find results in other languages with similar meaning!

### 2.3 Cross-Language Search

One of the most powerful features of semantic search is **cross-language capability**:

**Example Scenario**:
- A user searches in English: "laptop computer"
- The system can find products described in:
  - English: "Portable notebook PC"
  - French: "Ordinateur portable"
  - Spanish: "Computadora portátil"
  - German: "Tragbarer Computer"

This works because embeddings capture the **semantic meaning** across languages, not just the literal words.

---

## 3. Embedding Models

The project supports multiple embedding model providers. You configure them in `appsettings.json` under the `AI` section in the `Boilerplate.Server.Api` project.

### 3.1 LocalTextEmbeddingGenerationService (Default)

**Location**: [`src/Server/Boilerplate.Server.Api/Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs) (lines 367-372)

```csharp
services.AddEmbeddingGenerator(sp => new LocalTextEmbeddingGenerationService()
    .AsEmbeddingGenerator())
    .UseLogging()
    .UseOpenTelemetry();
```

**Characteristics**:
- Runs locally without external API calls
- Generates 384-dimensional vectors
- Uses a small model that runs on your server
- **Not recommended for production** due to limited accuracy
- Good for development and testing

### 3.2 Production-Recommended Models

For production environments, you should use more accurate models:

#### **OpenAI Embeddings**
**Configuration** in [`appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):
```json
"OpenAI": {
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingApiKey": "your-api-key",
    "EmbeddingEndpoint": "https://models.inference.ai.azure.com"
}
```

- **Models**: `text-embedding-3-small`, `text-embedding-3-large`
- **Pros**: High accuracy, widely supported
- **Cons**: Requires API calls, costs per token

#### **Azure OpenAI**
**Configuration** in `appsettings.json`:
```json
"AzureOpenAI": {
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingApiKey": "your-key",
    "EmbeddingEndpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment"
}
```

- **Pros**: Enterprise-grade, compliance, data residency control
- **Cons**: Requires Azure subscription, costs

#### **Hugging Face Models**
**Configuration** in `appsettings.json`:
```json
"HuggingFace": {
    "EmbeddingApiKey": "your-key",
    "EmbeddingEndpoint": "https://api-inference.huggingface.co/models/..."
}
```

- **Pros**: Open-source models, flexible, cost-effective
- **Cons**: May require more configuration, varying quality

### 3.3 Ollama (Self-Hosted Option)

You can also use **Ollama** to run embedding models locally on your own infrastructure:

- **Benefits**: Complete control, no external API calls, privacy
- **Models**: Many open-source embedding models available
- **Setup**: Install Ollama and configure the endpoint in `appsettings.json`

---

## 4. Enable Embeddings in the Project

By default, embeddings are **disabled** in the project. To enable them:

### Step 1: Open AppDbContext.cs

**File Location**: [`src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`](/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs)

**Current State** (line 158):
```csharp
// This requires SQL Server 2025+
public static readonly bool IsEmbeddingEnabled = false;
```

### Step 2: Change to Enable Embeddings

**Change to**:
```csharp
// This requires SQL Server 2025+
public static readonly bool IsEmbeddingEnabled = true;
```

### Step 3: Important Prerequisites

⚠️ **CRITICAL REQUIREMENT**: Vector embeddings require **SQL Server 2025+** which includes native vector search support.

If you're using an older version of SQL Server:
- **Option 1**: Upgrade to SQL Server 2025+
- **Option 2**: Switch to PostgreSQL with the `pgvector` extension
- **Option 3**: Use a different database with vector support (e.g., Azure Cosmos DB, Qdrant, etc.)

---

## 5. How Embeddings Work in the Project

### 5.1 The Product Entity

**File**: [`src/Server/Boilerplate.Server.Api/Models/Products/Product.cs`](/src/Server/Boilerplate.Server.Api/Models/Products/Product.cs)

The `Product` model includes an `Embedding` property:

```csharp
public Microsoft.Data.SqlTypes.SqlVector<float>? Embedding { get; set; }
```

This property stores the 384-dimensional vector representation of the product's data.

### 5.2 Product Configuration

**File**: [`src/Server/Boilerplate.Server.Api/Data/Configurations/Product/ProductConfiguration.cs`](/src/Server/Boilerplate.Server.Api/Data/Configurations/Product/ProductConfiguration.cs)

```csharp
if (AppDbContext.IsEmbeddingEnabled)
{
    builder.Property(p => p.Embedding).HasColumnType("vector(384)");
}
else
{
    builder.Ignore(p => p.Embedding);
}
```

- **When enabled**: Creates a `vector(384)` column in the database
- **When disabled**: Ignores the property (no database column created)
- **Dimensions**: 384 matches the `LocalTextEmbeddingGenerationService` output (adjust for other models)

### 5.3 ProductEmbeddingService

**File**: [`src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs`](/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs)

This service handles embedding generation and semantic search:

#### **Generating Embeddings**

The `Embed()` method creates embeddings from product data:

```csharp
public async Task Embed(Product product, CancellationToken cancellationToken)
{
    // Combine multiple fields with different weights
    List<(string text, float weight)> inputs =
    [
        ($"Id: {product.ShortId}", 0.9f),
        ($"Name: {product.Name}", 0.9f),
        (product.Category!.Name!, 0.9f)
    ];
    
    if (string.IsNullOrEmpty(product.DescriptionText) is false)
    {
        inputs.Add((product.DescriptionText, 0.7f));
    }
    
    // ...
    
    // Generate embeddings for all inputs, combine with weights, and normalize
    
    // ...
    
    product.Embedding = new(embedding);
}
```

**Key Points**:
- **Weighted Combination**: Different product fields have different importance (weights)
- **Product Name & ID**: Highest weight (0.9) - most important for search
- **Description**: Medium weight (0.7)
- **Alt Text**: Lower weight (0.5)
- **Category**: High weight (0.9)
- **Normalization**: L2 normalization ensures stable cosine distance calculations

#### **Searching with Embeddings**

The `SearchProducts()` method performs semantic search:

```csharp
public async Task<IQueryable<Product>> SearchProducts(string searchQuery, CancellationToken cancellationToken)
{
    if (AppDbContext.IsEmbeddingEnabled is false)
        throw new InvalidOperationException("Embeddings are not enabled.");
    
    // Generate embedding for the search query
    var embeddedSearchQuery = await embeddingGenerator.GenerateAsync(searchQuery, cancellationToken);
    
    // Search using cosine distance
    var value = new Microsoft.Data.SqlTypes.SqlVector<float>(embeddedSearchQuery.Vector);
    return dbContext.Products
        .Where(p => p.Embedding.HasValue && 
                    EF.Functions.VectorDistance("cosine", p.Embedding.Value, value) < DISTANCE_THRESHOLD)
        .OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!.Value, value!));
}
```

**Key Points**:
- **Distance Threshold**: `0.65f` - only returns products with cosine distance less than this
- **Cosine Distance**: Measures the angle between vectors (smaller = more similar)
- **Ordering**: Results are sorted by similarity (closest matches first)

### 5.4 Usage in ProductController

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

The controller calls the `productEmbeddingService.Embed()` method when creating or updating a product.

#### **Semantic Search**

```csharp
[HttpGet]
public async Task<IQueryable<ProductDto>> Search(string? searchQuery, ODataQueryOptions<ProductDto> odataQuery, CancellationToken cancellationToken)
{
    var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(
        (await productEmbeddingService.SearchProducts(searchQuery, cancellationToken)).Project(),
        new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.False }
    );
    
    return query;
}
```

---

## 6. Important Notes

### 6.1 Test Data Limitation

⚠️ **Important**: The test products that have been seeded into the database do NOT have embeddings. 

**Reason**: Embeddings are generated when products are created or updated through the API. The seeded data was added directly to the database without going through the embedding process.

**What This Means**:
- Searching for seeded test products using semantic search will **not** return any results
- Only products created/updated through the API will have embeddings and be searchable

**Solution**: 
- Create new products through the API to test semantic search
- Or manually generate embeddings for seeded products by updating them through the API

### 6.2 Performance Considerations

**Vector-Based Search**:
- More computationally expensive than keyword search
- Requires AI model API calls or local model execution
- Database vector operations require modern database versions

**Best Practice - Hybrid Approach**:
A hybrid approach can offer a balance between speed and accuracy. You can first perform a quick full-text search and, if the results are insufficient, fall back to a more comprehensive semantic search.

### 6.3 When to Use Semantic Search

**Semantic Search is Overkill for**:
- Simple product catalogs with straightforward names
- Exact match searches
- High-performance requirements with simple data

**Semantic Search is Ideal for**:
- Large content repositories (articles, documents, knowledge bases)
- Multi-language content
- Complex queries with synonyms and related concepts
- Recommendation systems
- Support ticket routing
- FAQ and chatbot systems

---

## 7. Real-World Example Scenario

**Scenario**: An e-commerce platform with products described in multiple languages.

**User Action**: A French-speaking user searches for "ordinateur portable gaming"

**What Happens**:
1. The search query is converted to a 384-dimensional vector
2. The system calculates cosine distance between the query vector and all product embeddings
3. Results include:
   - English products: "Gaming Laptop", "Portable Gaming PC"
   - French products: "PC portable pour jeux"
   - German products: "Gaming Notebook"
   - Spanish products: "Laptop para juegos"

**Traditional Keyword Search** would only find exact matches for "ordinateur portable gaming", missing all the semantically similar products in other languages.

---

## Summary

In this stage, you learned about:

✅ **Different search approaches**: Simple string matching, full-text search, vector-based semantic search, and hybrid approaches

✅ **Vector embeddings**: Numerical representations of text that capture semantic meaning

✅ **Semantic search capabilities**: Understanding meaning, finding synonyms, and cross-language searches

✅ **Embedding models**: LocalTextEmbeddingGenerationService (development), OpenAI, Azure OpenAI, and Hugging Face (production)

✅ **Enabling embeddings**: Changing `IsEmbeddingEnabled` to `true` in `AppDbContext.cs`

✅ **ProductEmbeddingService**: How embeddings are generated with weighted combinations and how semantic search works

✅ **Database requirements**: SQL Server 2025+ for native vector support

✅ **Usage in ProductController**: Embedding generation on create/update and semantic search implementation

✅ **Best practices**: When to use semantic search and hybrid approaches for optimal performance

---

**Additional Resources**:
- Microsoft.Extensions.AI documentation: https://learn.microsoft.com/en-us/dotnet/ai/
- OpenAI Embeddings Guide: https://platform.openai.com/docs/guides/embeddings
- Hugging Face Embeddings: https://huggingface.co/models?pipeline_tag=feature-extraction

Happy coding! 🚀

