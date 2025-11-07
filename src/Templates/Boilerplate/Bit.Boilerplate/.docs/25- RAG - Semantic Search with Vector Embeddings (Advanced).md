# Stage 25: RAG - Semantic Search with Vector Embeddings (Advanced)

Welcome to Stage 25! In this advanced stage, you'll learn about implementing **semantic search** using **vector embeddings** for database queries. This powerful technique enables meaning-based searches that go far beyond traditional keyword matching.

---

## Overview: Different Search Approaches

When implementing search functionality in applications, you have several approaches to choose from:

### 1. **Simple String Matching**
The most basic approach using methods like `Contains()`:
```csharp
products.Where(p => p.Name.Contains(searchQuery))
```

**Pros:** Simple, fast for small datasets  
**Cons:** Keyword-only matching, no understanding of meaning or synonyms

### 2. **Full-Text Search**
Database-native full-text search capabilities:
- **PostgreSQL**: Built-in full-text search with ranking
- **SQL Server**: Full-text indexes and search
- **MySQL**: FULLTEXT indexes

**Pros:** Better performance, supports stemming and ranking  
**Cons:** Still keyword-based, no semantic understanding

### 3. **Vector-Based Semantic Search** ⭐
Uses text embedding models to enable meaning-based searches:

**Pros:**
- Understands semantic meaning, not just keywords
- Finds similar concepts even with different words
- Supports cross-language search
- Handles synonyms and related terms automatically

**Cons:**
- Requires embedding generation and storage
- More complex setup
- Additional computational overhead

### 4. **Hybrid Approach** (Recommended for Production)
Combines full-text search with vector-based search:
1. Try full-text search first (fast, good for exact matches)
2. Fall back to vector search if results are insufficient
3. Optionally blend results from both approaches

---

## What are Vector Embeddings?

### The Concept
**Vector embeddings** are numerical representations (arrays of floats) that capture the semantic meaning of text. Similar meanings result in vectors that are mathematically close together in multi-dimensional space.

### Example
```text
"laptop computer"     → [0.23, 0.45, -0.12, 0.78, ...]
"notebook PC"         → [0.25, 0.43, -0.10, 0.76, ...]  ← Similar vector!
"car"                 → [-0.45, 0.12, 0.89, -0.23, ...] ← Very different!
```

### Key Capabilities

#### Semantic Understanding
Searching for "laptop computer" will also find results containing:
- "notebook PC"
- "portable workstation"
- "mobile computer"

Even though the exact words don't match!

#### Cross-Language Search 🌍
A user can search in **English** and find results in **French**, **Spanish**, or any other language because embeddings capture **meaning**, not just words.

**Example:**
- Search: "luxury car" (English)
- Finds: "voiture de luxe" (French), "coche de lujo" (Spanish)

#### Contextual Relevance
The search understands context and relationships between concepts, making results more relevant to user intent.

---

## Embedding Models

### 1. LocalTextEmbeddingGenerationService (Default)

**What it is:**  
A local embedding model included in the project by default using `SmartComponents.LocalEmbeddings.SemanticKernel`.

**Characteristics:**
- ✅ Runs locally - no API calls needed
- ✅ Free and private
- ✅ Good for development and testing
- ⚠️ **Limited accuracy** compared to cloud models
- ⚠️ **Not recommended for production** use

**When to use:**  
Perfect for learning, development, and prototyping.

---

### 2. OpenAI Embeddings (Recommended for Production)

**Model:** `text-embedding-3-small`

**Characteristics:**
- ⭐ High-quality embeddings
- ⭐ Excellent accuracy and semantic understanding
- ⭐ Cost-effective
- 💰 Pay-per-use pricing
- 🌐 Requires API key and internet connection

**Configuration in [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):**
```json
"AI": {
  "OpenAI": {
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingApiKey": "your-api-key-here",
    "EmbeddingApiKey_Comment": "Get one at https://github.com/settings/personal-access-tokens/new",
    "EmbeddingEndpoint": "https://models.inference.ai.azure.com"
  }
}
```

**Get an API Key:**  
Visit [GitHub Models](https://github.com/marketplace/models) or [OpenAI Platform](https://platform.openai.com/api-keys)

---

### 3. Azure OpenAI Embeddings (Enterprise)

**Model:** `text-embedding-3-small` (or custom deployment)

**Characteristics:**
- 🏢 Enterprise-grade with SLA
- 🔒 Enhanced security and compliance
- 📊 Better monitoring and analytics
- ⚙️ Full control over deployment region
- 💰 Requires Azure subscription

**Configuration in [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):**
```json
"AI": {
  "AzureOpenAI": {
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingApiKey": "your-azure-key",
    "EmbeddingApiKey_Comment": "Get one at https://portal.azure.com",
    "EmbeddingEndpoint": "https://yourResourceName.openai.azure.com/openai/deployments/yourDeployment"
  }
}
```

---

### 4. Hugging Face Models (Open Source)

**Characteristics:**
- 🆓 Open-source models
- 🔧 Highly customizable
- 🌍 Large community and model variety
- 📚 Good for research and experimentation

**Configuration in [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json):**
```json
"AI": {
  "HuggingFace": {
    "EmbeddingApiKey": "your-huggingface-token",
    "EmbeddingEndpoint": "https://api-inference.huggingface.co/models/sentence-transformers/all-MiniLM-L6-v2"
  }
}
```

---

## Database Support

### PostgreSQL with pgvector Extension

**Setup Requirements:**

1. **Install pgvector extension:**

If using **Docker**:
```bash
docker run -d --name postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=BoilerplateDb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  --restart unless-stopped \
  pgvector/pgvector:pg18
```

If using **.NET Aspire** (from [`/src/Server/Boilerplate.Server.AppHost/Program.cs`](/src/Server/Boilerplate.Server.AppHost/Program.cs)):
```csharp
var postgresDatabase = builder.AddPostgres("postgresserver")
    .WithPgAdmin(config => config.WithVolume("/var/lib/pgadmin/Boilerplate/data"))
    .WithDataVolume()
    .WithImage("pgvector/pgvector", "pg18") // ← pgvector support built-in
    .AddDatabase("postgresdb");
```

2. **NuGet Package:**
```xml
<PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.2" />
```

**Vector Distance Functions:**
- `CosineDistance()` - Recommended for semantic similarity
- `L2Distance()` - Euclidean distance
- `MaxInnerProduct()` - Dot product similarity

---

### SQL Server with Vector Support

**Requirements:**  
SQL Server 2025+ includes native vector support.

**Setup:**
```csharp
var sqlDatabase = builder.AddSqlServer("sqlserver")
    .WithDbGate(config => config.WithDataVolume())
    .WithDataVolume()
    .WithImage("mssql/server", "2025-latest") // ← Vector support in 2025+
    .AddDatabase("mssqldb");
```

**Vector Functions:**
- `EF.Functions.VectorDistance("cosine", vector1, vector2)`

---

## Enabling Embeddings in the Project

### Step 1: Enable in AppDbContext

**File:** [`/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs`](/src/Server/Boilerplate.Server.Api/Data/AppDbContext.cs)

**Find this line (near line 279):**
```csharp
public static readonly bool IsEmbeddingEnabled = false;
```

**Change it to:**
```csharp
public static readonly bool IsEmbeddingEnabled = true;
```

**What this does:**
- Enables the `Embedding` property on the `Product` entity
- Configures the database to store vector columns
- Activates embedding generation when products are created/updated

---

### Step 2: Configure Embedding Model

**File:** [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

Choose your preferred embedding provider (see **Embedding Models** section above) and configure the API keys and endpoints.

---

### Step 3: Run Migration (If Needed)

If you've already created the database, you'll need to create a new migration to add the embedding column:

```bash
cd src/Server/Boilerplate.Server.Api
dotnet ef migrations add AddProductEmbeddings --verbose
dotnet ef database update
```

---

## Implementation Details

### Product Entity with Embedding

**File:** [`/src/Server/Boilerplate.Server.Api/Models/Products/Product.cs`](/src/Server/Boilerplate.Server.Api/Models/Products/Product.cs)

```csharp
public partial class Product
{
    public Guid Id { get; set; }
    public int ShortId { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public string? DescriptionHTML { get; set; }
    public string? DescriptionText { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Vector embedding for semantic search
    //#if (database == "PostgreSQL")
    public Pgvector.Vector? Embedding { get; set; }
    //#elif (database == "SqlServer")
    public Microsoft.Data.SqlTypes.SqlVector<float>? Embedding { get; set; }
    //#endif
}
```

---

### Entity Configuration

**File:** [`/src/Server/Boilerplate.Server.Api/Data/Configurations/Product/ProductConfiguration.cs`](/src/Server/Boilerplate.Server.Api/Data/Configurations/Product/ProductConfiguration.cs)

```csharp
public void Configure(EntityTypeBuilder<Product> builder)
{
    builder.HasIndex(p => p.Name).IsUnique();
    builder.HasIndex(p => p.ShortId).IsUnique();
    
    if (AppDbContext.IsEmbeddingEnabled)
    {
        // Dimensions depend on the embedding model
        // LocalTextEmbeddingGenerationService uses 384 dimensions
        // text-embedding-3-small uses 1536 dimensions
        builder.Property(p => p.Embedding).HasColumnType("vector(384)");
    }
    else
    {
        builder.Ignore(p => p.Embedding);
    }
}
```

**Important:** Adjust the vector dimension (`vector(384)`) based on your embedding model:
- `LocalTextEmbeddingGenerationService`: 384
- `text-embedding-3-small`: 1536
- `text-embedding-3-large`: 3072

---

### ProductEmbeddingService

**File:** [`/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs`](/src/Server/Boilerplate.Server.Api/Services/ProductEmbeddingService.cs)

This service handles:
1. **Generating embeddings** for products
2. **Searching products** using semantic similarity

#### Key Methods

##### 1. SearchProducts Method

```csharp
public async Task<IQueryable<Product>> SearchProducts(
    string searchQuery, 
    CancellationToken cancellationToken)
{
    if (AppDbContext.IsEmbeddingEnabled is false)
        throw new InvalidOperationException("Embeddings are not enabled.");
    
    // Generate embedding for the search query
    var embeddedSearchQuery = await embeddingGenerator.GenerateAsync(
        searchQuery, 
        cancellationToken: cancellationToken);
    
    // PostgreSQL
    var value = new Pgvector.Vector(embeddedSearchQuery.Vector);
    
    // Return products ordered by similarity (cosine distance)
    return dbContext.Products
        .Where(p => p.Embedding!.CosineDistance(value) < DISTANCE_THRESHOLD)
        .OrderBy(p => p.Embedding!.CosineDistance(value));
}
```

**Distance Threshold:**
```csharp
private const float DISTANCE_THRESHOLD = 0.65f;
```
- Values closer to `0.0` = more similar
- Values closer to `1.0` = less similar
- Adjust threshold based on your needs

---

##### 2. Embed Method (Weighted Embeddings)

```csharp
public async Task Embed(Product product, CancellationToken cancellationToken)
{
    if (AppDbContext.IsEmbeddingEnabled is false)
        throw new InvalidOperationException("Embeddings are not enabled.");
    
    // Load related category
    await dbContext.Entry(product)
        .Reference(p => p.Category)
        .LoadAsync(cancellationToken);
    
    // Create weighted inputs for better semantic representation
    List<(string text, float weight)> inputs = [
        ($"Id: {product.ShortId}", 0.9f),              // High weight
        ($"Name: {product.Name}", 0.9f),               // High weight
        (product.DescriptionText ?? "", 0.7f),         // Medium weight
        (product.PrimaryImageAltText ?? "", 0.5f),     // Lower weight
        (product.Category!.Name!, 0.9f)                // High weight
    ];
    
    // Generate embeddings for each input
    var texts = inputs.Select(i => i.text).ToArray();
    var embeddingsResponse = await embeddingGenerator.GenerateAsync(
        texts, 
        cancellationToken: cancellationToken);
    
    // Combine embeddings using weights
    var vectors = embeddingsResponse.Select(e => e.Vector.ToArray()).ToArray();
    var weights = inputs.Select(t => t.weight).ToArray();
    
    var embedding = new float[vectors[0].Length];
    for (int i = 0; i < embedding.Length; i++)
    {
        embedding[i] = 0f;
        for (int j = 0; j < vectors.Length; j++)
        {
            embedding[i] += weights[j] * vectors[j][i];
        }
    }
    
    // L2 normalize for cosine distance stability
    float norm = (float)Math.Sqrt(embedding.Sum(v => v * v));
    if (norm > 0)
    {
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= norm;
        }
    }
    
    // Store the embedding
    product.Embedding = new Pgvector.Vector(embedding);
}
```

**Why Weighted Embeddings?**
- **Product Name** and **Category** are more important (weight: 0.9)
- **Description** provides context (weight: 0.7)
- **Alt Text** is less critical (weight: 0.5)

This approach creates more accurate semantic representations than treating all fields equally.

---

### API Controller Integration

**File:** [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

```csharp
[HttpGet("{searchQuery}")]
public async Task<PagedResult<ProductDto>> SearchProducts(
    string searchQuery, 
    ODataQueryOptions<ProductDto> odataQuery, 
    CancellationToken cancellationToken)
{
    // Get semantic search results
    var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(
        (await productEmbeddingService.SearchProducts(searchQuery, cancellationToken))
            .Project(),
        ignoreQueryOptions: AllowedQueryOptions.Top 
                          | AllowedQueryOptions.Skip 
                          | AllowedQueryOptions.OrderBy); // Don't disturb semantic ordering
    
    var totalCount = await query.LongCountAsync(cancellationToken);
    
    // Apply pagination
    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);
    
    return new PagedResult<ProductDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount);
}
```

**Key Points:**
- Semantic ordering is preserved (don't apply OData `$orderby`)
- OData filtering, pagination still work
- Returns `PagedResult` for grid compatibility

---

### Client-Side Usage

**File:** [`/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs)

```csharp
private BitDataGridItemsProvider<ProductDto> productsProvider = default!;

protected override async Task OnInitAsync()
{
    productsProvider = async req =>
    {
        try
        {
            isLoading = true;
            
            var query = new ODataQueryBuilder();
            query.Top = req.Count ?? 10;
            query.Skip = req.StartIndex;
            
            // Apply filters
            if (!string.IsNullOrEmpty(ProductNameFilter))
            {
                query.Filter = $"contains(tolower({nameof(ProductDto.Name)}),'{ProductNameFilter.ToLower()}')";
            }
            
            var queriedRequest = productController.WithQuery(query.ToString());
            
            // Use semantic search if search query is present
            var data = await (string.IsNullOrWhiteSpace(searchQuery)
                ? queriedRequest.GetProducts(req.CancellationToken)
                : queriedRequest.SearchProducts(searchQuery, req.CancellationToken));
            
            return BitDataGridItemsProviderResult.From(
                data!.Items!, 
                (int)data!.TotalCount);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return BitDataGridItemsProviderResult.From(new List<ProductDto> { }, 0);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    };
}
```

**UI Experience:**
- Normal browsing: Uses traditional OData queries
- Search active: Switches to semantic search automatically
- Seamless integration with `BitDataGrid`

---

## AI Chatbot Integration

### RAG (Retrieval-Augmented Generation)

The semantic search capabilities can be integrated with AI chatbots for **Retrieval-Augmented Generation (RAG)**.

**File:** [`/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs)

```csharp
[Description("Get car recommendations based on user needs")]
async Task<object?> GetProductRecommendations(
    [Description("User's needs description")] string userNeeds,
    [Description("Car manufacturer (Optional)")] string? manufacturer,
    [Description("Max price (Optional)")] decimal? maxPrice,
    [Description("Min price (Optional)")] decimal? minPrice)
{
    var scope = serviceProvider.CreateAsyncScope();
    var productEmbeddingService = scope.ServiceProvider
        .GetRequiredService<ProductEmbeddingService>();
    
    // Build search query
    var searchQuery = string.IsNullOrWhiteSpace(manufacturer)
        ? userNeeds
        : $"**{manufacturer}** {userNeeds}";
    
    // Semantic search + filtering
    var recommendedProducts = await (await productEmbeddingService
        .SearchProducts(searchQuery, cancellationToken))
        .WhereIf(maxPrice.HasValue, p => p.Price <= maxPrice!.Value)
        .WhereIf(minPrice.HasValue, p => p.Price >= minPrice!.Value)
        .Take(10)
        .Project()
        .Select(p => new
        {
            p.Name,
            p.PageUrl,
            Manufacturer = p.CategoryName,
            Price = p.FormattedPrice,
            Description = p.DescriptionText,
            PreviewImageUrl = p.GetPrimaryMediumImageUrl(serverAddress) 
                ?? "_content/Boilerplate.Client.Core/images/car_placeholder.png"
        })
        .ToArrayAsync(cancellationToken);
    
    return recommendedProducts;
}
```

**How RAG Works:**
1. User asks chatbot: *"I need a reliable family SUV with good fuel economy"*
2. Chatbot calls `GetProductRecommendations` tool
3. Semantic search finds relevant vehicles (even if they don't contain exact keywords)
4. Results are passed to the AI model
5. Chatbot provides natural language recommendations with accurate product data

**Benefits:**
- AI has access to real, up-to-date product data
- Reduces hallucinations (AI making up facts)
- Combines semantic understanding with structured data
- Filters work alongside semantic search (price, category, etc.)

---

## Performance Considerations

### Indexing

**PostgreSQL with pgvector:**
```sql
CREATE INDEX ON products USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);
```

**Why Indexes Matter:**
- Without index: Full table scan for every search
- With index: Fast approximate nearest neighbor (ANN) search
- Trade-off: Slight reduction in accuracy for major speed improvement

---

### Caching Embeddings

**Best Practice:**
- Generate embeddings once when product is created/updated
- Store embeddings in the database
- Search queries generate embedding on-the-fly (fast, single query)

**Do NOT:**
- Generate embeddings for all products on every search
- Store embeddings only in memory

---

### Hybrid Search Strategy

**Recommended Production Pattern:**

```csharp
public async Task<IQueryable<Product>> SmartSearch(
    string searchQuery, 
    CancellationToken cancellationToken)
{
    // Try full-text search first (fast)
    var fullTextResults = await FullTextSearch(searchQuery, cancellationToken);
    
    // If enough results, return them
    if (fullTextResults.Count() >= 10)
        return fullTextResults;
    
    // Otherwise, use semantic search
    return await SearchProducts(searchQuery, cancellationToken);
}
```

This approach:
- Leverages fast full-text search for exact matches
- Falls back to semantic search for complex queries
- Optimizes both performance and accuracy

---

## Testing Semantic Search

### 1. Enable Embeddings
Follow the steps in **Enabling Embeddings in the Project** section above.

### 2. Seed Data with Embeddings

When you create new products through the API, embeddings are automatically generated:

```csharp
[HttpPost]
public async Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken)
{
    var entity = dto.Map();
    
    // Generate embedding for the new product
    await productEmbeddingService.Embed(entity, cancellationToken);
    
    await DbContext.Products.AddAsync(entity, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
    
    return entity.Map();
}
```

**Note:** Seeded test data in [`ProductConfiguration.cs`](/src/Server/Boilerplate.Server.Api/Data/Configurations/Product/ProductConfiguration.cs) does not have embeddings by default. You'll need to create new products via the API to test semantic search.

---

### 3. Try Semantic Searches

**Example Queries:**

| Search Query | What It Finds | Why It Works |
|-------------|---------------|--------------|
| "family SUV" | GLC SUV, Explorer, Pathfinder | Understands "family" → spacious, safe, versatile |
| "luxury sedan" | S-Class, E-Class, Model S | Semantic match for premium vehicles |
| "electric car" | Model 3, Model Y, EQE, i4 | Recognizes EVs across brands |
| "affordable truck" | Maverick, F-150 (base) | Considers price + vehicle type |
| "sports car" | Mustang, Z, M3, AMG GT | Performance-oriented vehicles |

**Cross-Language Example:**
- Search: "voiture électrique" (French for "electric car")
- Finds: Tesla Model 3, BMW i4, Mercedes EQE
- Even though product descriptions are in English!

---

## Troubleshooting

### Embeddings Not Working

**Check:**
1. `IsEmbeddingEnabled = true` in `AppDbContext.cs`
2. Correct embedding model configured in `appsettings.json`
3. Valid API key for cloud embedding services
4. Database has `vector` column type support
5. Products have embeddings (check database)

---

### Poor Search Results

**Possible Issues:**
1. **Threshold too strict:** Increase `DISTANCE_THRESHOLD` from 0.65 to 0.75
2. **Wrong vector dimensions:** Ensure configuration matches model (384 vs 1536)
3. **Low-quality embeddings:** Use production embedding model instead of local model
4. **Not enough data:** Need sufficient products with embeddings for good results

---

### Performance Issues

**Solutions:**
1. Create vector indexes (see **Performance Considerations**)
2. Implement caching for common searches
3. Use hybrid search approach
4. Reduce vector dimensions (if model supports it)
5. Limit result set size

---

## Summary

You've learned about:

✅ **Different search approaches** (string matching, full-text, vector, hybrid)  
✅ **Vector embeddings** and how they capture semantic meaning  
✅ **Embedding models** (local, OpenAI, Azure, Hugging Face)  
✅ **Database support** (PostgreSQL pgvector, SQL Server 2025+)  
✅ **Implementation** with `ProductEmbeddingService`  
✅ **Weighted embeddings** for better accuracy  
✅ **API integration** with OData support  
✅ **RAG integration** with AI chatbots  
✅ **Performance optimization** strategies  
✅ **Testing and troubleshooting**

### When to Use Semantic Search

**Great for:**
- 🔍 Complex product catalogs
- 📄 Document search systems
- 💬 Customer support knowledge bases
- 🎓 Educational content search
- 🌐 Multi-language platforms

**Overkill for:**
- 📋 Simple lists with few items
- 🔢 Exact ID or code lookups
- 📊 Structured data with clear fields
- ⚡ When millisecond response time is critical

---

### Next Steps

1. **Experiment** with different embedding models
2. **Implement** hybrid search in your project
3. **Measure** search quality and performance
4. **Integrate** with AI chatbot (RAG pattern)
5. **Optimize** indexes and caching

---

## Additional Resources

- **pgvector Documentation:** https://github.com/pgvector/pgvector
- **Microsoft.Extensions.AI:** https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/
- **OpenAI Embeddings:** https://platform.openai.com/docs/guides/embeddings
- **Semantic Kernel:** https://learn.microsoft.com/en-us/semantic-kernel/overview/

---
