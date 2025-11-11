# Stage 3: API Controllers and OData

Welcome to Stage 3! In this stage, you'll learn how the project implements API controllers, uses OData for powerful query capabilities, and follows best practices for building efficient, secure, and scalable APIs.

This stage provides a comprehensive exploration of the backend API architecture, demonstrating real examples from the actual project codebase with clickable, navigable file references.

---

## Table of Contents

1. [Controller Architecture](#controller-architecture)
2. [Dependency Injection with [AutoInject]](#dependency-injection-with-autoinject)
3. [Reading Data with IQueryable](#reading-data-with-iqueryable)
4. [OData Query Options Support](#odata-query-options-support)
5. [PagedResult for Total Count](#pagedresult-for-total-count)
6. [Data Security and Permissions](#data-security-and-permissions)
7. [Live Demos](#live-demos)
8. [Performance Optimization](#performance-optimization)
9. [Real Usage Examples](#real-usage-examples)
10. [Proxy Interface Pattern](#proxy-interface-pattern)
11. [Architectural Philosophy](#architectural-philosophy)

---

## Controller Architecture

All API controllers in this project inherit from `AppControllerBase`, which is located at:

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs`](/src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs)

```csharp
namespace Boilerplate.Server.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
```

---

## Dependency Injection with [AutoInject]

One of the most powerful features in this project is the `[AutoInject]` attribute, which simplifies dependency injection.

### Traditional Constructor vs Primary Constructor vs [AutoInject]

**Traditional Constructor**:

```csharp
public class ProductController : AppControllerBase
{
    private readonly AppDbContext dbContext;
    private readonly IStringLocalizer<AppStrings> localizer;
    private readonly ServerApiSettings settings;
    private readonly HtmlSanitizer htmlSanitizer;
    
    public ProductController(
        AppDbContext dbContext,
        IStringLocalizer<AppStrings> localizer,
        ServerApiSettings settings,
        HtmlSanitizer htmlSanitizer): base(dbContext, localizer, settings)
    {
        this.dbContext = dbContext;
        this.localizer = localizer;
        this.settings = settings;
        this.htmlSanitizer = htmlSanitizer;
    }
}
```

**Primary Constructor**:

```csharp
public class ProductController(
        AppDbContext dbContext,
        IStringLocalizer<AppStrings> localizer,
        ServerApiSettings settings,
        HtmlSanitizer htmlSanitizer) : AppControllerBase(dbContext, localizer, settings)
{
}
```

**AutoInject**:

```csharp
public partial class ProductController : AppControllerBase
{
    [AutoInject] private HtmlSanitizer htmlSanitizer = default!;
}
```

### Key Benefits of [AutoInject]

1. **No Repetitive Constructor Code**: You don't need to write lengthy constructors.
2. **Automatic Inheritance**: Dependencies already injected in base classes (like `DbContext`, `Localizer`, `AppSettings` in `AppControllerBase`) are automatically available in derived classes without redeclaring them.
3. **Cleaner Code**: Less boilerplate, more focus on business logic.

### Example from CategoryController

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

```csharp
public partial class CategoryController : AppControllerBase, ICategoryController
{
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;

    [HttpGet, EnableQuery]
    public IQueryable<CategoryDto> Get()
    {
        // DbContext and Localizer are inherited from AppControllerBase
        return DbContext.Categories.Project();
    }
}
```

Notice how the controller uses `DbContext` and `Localizer` without explicitly injecting them - they're inherited from `AppControllerBase`.

---

## Reading Data with IQueryable

The recommended pattern for reading data in this project is to **return `IQueryable<DTO>`** from your API endpoints.

### Why IQueryable?

When you return `IQueryable<T>` combined with the `[EnableQuery]` attribute, you enable:

- **Filtering** with `$filter`
- **Sorting** with `$orderby`
- **Pagination** with `$top` and `$skip`
- **Selecting specific properties/columns** with `$select`
- **Expanding (Including) related entities** with `$expand`

This means the client can query the API flexibly without requiring new endpoints for every filter/sort combination.

### Example: Simple Get Method

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

```csharp
[HttpGet, EnableQuery]
public IQueryable<CategoryDto> Get()
{
    return DbContext.Categories
        .Project(); // Uses Mapperly for efficient projection
}
```

The `[EnableQuery]` attribute automatically processes OData query parameters from the request URL and applies them to the `IQueryable` before execution.

---

## OData Query Options Support

This project supports most OData query options, allowing clients to create powerful, flexible queries.

### Supported OData Query Options

✅ **Supported**:
- `$top` - Limit the number of results
- `$skip` - Skip a number of results (pagination)
- `$filter` - Filter results based on conditions
- `$orderby` - Sort results
- `$select` - Select specific fields
- `$expand` - Include related entities
- `$search` - Full-text search

❌ **Not Supported Yet**:
- `$count` - Get total count as a separate query

### OData Query Examples

Here are practical examples of how clients can query your APIs:

#### 1. Top (Limit Results)
```
GET /api/Category/Get?$top=5
```
Returns only the first 5 categories.

#### 2. Skip (Pagination)
```
GET /api/Category/Get?$skip=10&$top=10
```
Returns 10 categories, starting from the 11th category (useful for page 2 with 10 items per page).

#### 3. Filter (Conditional Query)
```
GET /api/Product/Get?$filter=Price gt 100
```
Returns products with a price greater than 100.

**Complex Filter Example**:
```
GET /api/Product/Get?$filter=contains(tolower(Name),'phone') and Price lt 500
```
Returns products with "phone" in the name and price less than 500.

#### 4. OrderBy (Sorting)
```
GET /api/Product/Get?$orderby=Name desc
```
Returns products sorted by name in descending order.

**Multiple Sort Fields**:
```
GET /api/Product/Get?$orderby=CategoryName asc, Price desc
```

#### 5. Combined Queries
```
GET /api/Product/Get?$filter=Price gt 50&$orderby=Name&$top=20&$skip=0
```
Returns the first 20 products with price greater than 50, sorted by name.

### OData Query Building

The project includes an `ODataQuery` helper class for building queries programmatically.

**File**: [`src/Client/Boilerplate.Client.Core/Services/ODataQuery.cs`](/src/Client/Boilerplate.Client.Core/Services/ODataQuery.cs)

```csharp
public partial class ODataQuery
{
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
    public string? Select { get; set; }
    public string? Expand { get; set; }
    public string? Search { get; set; }
    
    public override string? ToString()
    {
        // Builds query string like "$top=10&$skip=0&$filter=..."
    }
}
```

### Real Client-Side Usage Example

**File**: [`src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs)

```csharp
private void PrepareGridDataProvider()
{
    productsProvider = async req =>
    {
        var query = new ODataQuery
        {
            Top = req.Count ?? 10,
            Skip = req.StartIndex,
            OrderBy = string.Join(", ", req.GetSortByProperties()
                .Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
        };

        if (string.IsNullOrEmpty(ProductNameFilter) is false)
        {
            query.Filter = $"contains(tolower({nameof(ProductDto.Name)}),'{ProductNameFilter.ToLower()}')";
        }

        if (string.IsNullOrEmpty(CategoryNameFilter) is false)
        {
            query.AndFilter = $"contains(tolower({nameof(ProductDto.CategoryName)}),'{CategoryNameFilter.ToLower()}')";
        }

        var queriedRequest = productController.WithQuery(query.ToString());
        var data = await queriedRequest.GetProducts(req.CancellationToken);

        return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
    };
}
```

This code:
1. Creates an `ODataQuery` object with pagination, sorting, and filtering
2. Applies it to the API request using `WithQuery()`
3. Gets a `PagedResult` containing both the data and total count

---

## PagedResult for Total Count

When a client (like a data grid) needs to know both the **page data** AND the **total count** of records, use `PagedResult<T>`.

### The PagedResult Class

**File**: [`src/Shared/Dtos/PagedResultDto.cs`](/src/Shared/Dtos/PagedResultDto.cs)

```csharp
public partial class PagedResult<T>
{
    public T[] Items { get; set; } = [];
    public long TotalCount { get; set; }

    [JsonConstructor]
    public PagedResult(T[] items, long totalCount)
    {
        Items = items;
        TotalCount = totalCount;
    }
}
```

### Why Use PagedResult?

Without total count, the client doesn't know:
- How many pages exist
- Whether to show "Next Page" button
- Progress indicators like "Showing 10 of 250 items"

### Server-Side Implementation

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

```csharp
[HttpGet]
public async Task<PagedResult<CategoryDto>> GetCategories(
    ODataQueryOptions<CategoryDto> odataQuery, 
    CancellationToken cancellationToken)
{
    // Apply filtering and sorting, but NOT $top/$skip yet
    var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(
        Get(), 
        ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

    // Get total count BEFORE pagination
    var totalCount = await query.LongCountAsync(cancellationToken);

    // Now apply pagination
    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

    // Return both data and count
    return new PagedResult<CategoryDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount);
}
```

### Key Steps:
1. Apply filters/sorting but ignore `$top`/`$skip`
2. Count the total filtered results
3. Apply pagination
4. Return data + count in `PagedResult`

---

## Data Security and Permissions

**IMPORTANT**: The client can ONLY receive data they have permission to access.

### Authorization Example

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

```csharp
[ApiController, Route("api/[controller]/[action]"),
    Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS),
    Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    // All methods require PRIVILEGED_ACCESS and ManageProductCatalog permissions
}
```

### Security Guarantees

Even if a malicious client tries to manipulate OData query parameters, they **CANNOT**:
- Access data they don't have permission to see
- Bypass server-side authorization checks
- Execute arbitrary database queries
- Access soft-deleted or hidden records

### How It Works

1. **Authorization happens BEFORE OData processing**: The `[Authorize]` attributes are checked first.
2. **Query is scoped to user's permissions**: You can add user-specific filters in the controller method.
3. **Entity Framework prevents SQL injection**: OData parameters are safely translated to SQL.

### Example: User-Specific Data

```csharp
[HttpGet, EnableQuery]
public IQueryable<TodoItemDto> GetMyTodos()
{
    var userId = User.GetUserId(); // Get current user ID
    
    return DbContext.TodoItems
        .Where(t => t.UserId == userId) // Only return current user's todos
        .Project();
}
```

No matter what OData parameters the client sends, they can only see their own todos.

---

## Live Demos

### Demo 1: Admin Panel with OData

Visit the live admin panel to see OData in action:

🔗 **[https://adminpanel.bitplatform.dev/categories](https://adminpanel.bitplatform.dev/categories)**

This demo shows:
- Real-time filtering by category name
- Sorting by clicking column headers
- Pagination with page size selection
- All powered by OData queries

### Demo 2: Direct API Query

You can also call the API directly to see OData query strings:

🔗 **[https://sales.bitplatform.dev/api/ProductView/Get?$top=10&$skip=10&$orderby=Name](https://sales.bitplatform.dev/api/ProductView/Get?$top=10&$skip=10&$orderby=Name)**

This URL:
- Returns the second page of products (`$skip=10`)
- Shows 10 products per page (`$top=10`)
- Sorts by product name (`$orderby=Name`)

---

## Performance Optimization

The combination of `IQueryable`, Entity Framework Core, and OData provides excellent performance characteristics.

### Key Performance Benefits

#### 1. Direct SQL Translation
When you use `IQueryable` with `[EnableQuery]`, the OData parameters are translated directly into SQL:

```csharp
// Controller code
[HttpGet, EnableQuery]
public IQueryable<ProductDto> Get()
{
    return DbContext.Products.Project();
}

// Client request
GET /api/Product/Get?$top=10&$skip=10&$orderby=Name

// Generated SQL (simplified)
SELECT TOP 10 p.Id, p.Name, p.Price, c.Name as CategoryName
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
ORDER BY p.Name
OFFSET 10 ROWS
```

#### 2. Minimal Memory Consumption
- Only the requested 10 products are loaded into memory
- NOT loading all products and then filtering/sorting in C#
- Projection happens at the database level using `Project()`

#### 3. No Over-Fetching
- Only DTO properties are fetched, not the entire entity
- Related entities are joined efficiently

### Example: Efficient Product Query

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

```csharp
[HttpGet, EnableQuery]
public IQueryable<ProductDto> Get()
{
    return DbContext.Products
        .Project(); // Mapperly projection
}
```

**What happens when client requests:**
```
GET /api/Product/Get?$top=10&$skip=20&$orderby=Price desc&$filter=Price gt 50
```

1. SQL query is generated with `WHERE Price > 50`
2. Results are sorted by `Price DESC` in the database
3. Database skips 20 rows and returns 10 rows
4. Only DTO properties are selected (not full entities)
5. Total memory usage: ~10 DTO objects

### Scalability

This pattern scales efficiently even with **millions of records**:
- The database handles filtering and sorting
- Only the requested page is transferred over the network
- Client receives only the data it needs

**Example**: If you have 1 million products in the database:
- Getting page 1,000 (rows 10,000-10,010) is just as fast as getting page 1
- Memory consumption is constant regardless of database size
- Network traffic is constant (only 10 products per page)

---

## Real Usage Examples

Let's examine real controllers from the project to see these patterns in action.

### Example 1: CategoryController

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

This controller demonstrates:

#### Simple IQueryable GET
```csharp
[HttpGet, EnableQuery]
public IQueryable<CategoryDto> Get()
{
    return DbContext.Categories.Project();
}
```

#### PagedResult with Total Count
```csharp
[HttpGet]
public async Task<PagedResult<CategoryDto>> GetCategories(
    ODataQueryOptions<CategoryDto> odataQuery, 
    CancellationToken cancellationToken)
{
    var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(
        Get(), 
        ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);

    var totalCount = await query.LongCountAsync(cancellationToken);

    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

    return new PagedResult<CategoryDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount);
}
```

#### Single Item GET with Error Handling
```csharp
[HttpGet("{id}")]
public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
{
    var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    if (dto is null)
        throw new ResourceNotFoundException(
            Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

    return dto;
}
```

#### Create with Validation
```csharp
[HttpPost]
public async Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToAdd = dto.Map(); // Mapperly

    await DbContext.Categories.AddAsync(entityToAdd, cancellationToken);

    await Validate(entityToAdd, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);

    await PublishDashboardDataChanged(cancellationToken);

    return entityToAdd.Map();
}
```

#### Update with Concurrency Check
```csharp
[HttpPut]
public async Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToUpdate = await DbContext.Categories.FindAsync([dto.Id], cancellationToken)
        ?? throw new ResourceNotFoundException(
            Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

    dto.Patch(entityToUpdate); // Mapperly partial update

    await Validate(entityToUpdate, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);

    await PublishDashboardDataChanged(cancellationToken);

    return entityToUpdate.Map();
}
```

#### Delete with Business Logic Validation
```csharp
[HttpDelete("{id}/{concurrencyStamp}")]
public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
{
    // Business rule: Cannot delete category if it has products
    if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
    {
        throw new BadRequestException(Localizer[nameof(AppStrings.CategoryNotEmpty)]);
    }

    DbContext.Categories.Remove(new() 
    { 
        Id = id, 
        ConcurrencyStamp = Convert.FromHexString(concurrencyStamp) 
    });

    await DbContext.SaveChangesAsync(cancellationToken);

    await PublishDashboardDataChanged(cancellationToken);
}
```

#### Remote Validation
```csharp
private async Task Validate(Category category, CancellationToken cancellationToken)
{
    var entry = DbContext.Entry(category);
    
    // Check for duplicate category names
    if ((entry.State is EntityState.Added || entry.Property(c => c.Name).IsModified)
        && await DbContext.Categories.AnyAsync(p => p.Name == category.Name, cancellationToken))
        throw new ResourceValidationException(
            (nameof(CategoryDto.Name), 
             [Localizer[nameof(AppStrings.DuplicateCategoryName), category.Name!]]));
}
```

### Example 2: ProductController

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

This controller shows more advanced features:

#### Semantic Search Integration
```csharp
[HttpGet("{searchQuery}")]
public async Task<PagedResult<ProductDto>> SearchProducts(
    string searchQuery, 
    ODataQueryOptions<ProductDto> odataQuery, 
    CancellationToken cancellationToken)
{
    // Use embedding service for AI-powered search
    var searchResults = await productEmbeddingService.SearchProducts(searchQuery, cancellationToken);
    
    var query = (IQueryable<ProductDto>)odataQuery.ApplyTo(
        searchResults.Project(),
        ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.OrderBy);
        
    var totalCount = await query.LongCountAsync(cancellationToken);

    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

    return new PagedResult<ProductDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount);
}
```

#### HTML Sanitization
```csharp
[AutoInject] private HtmlSanitizer htmlSanitizer = default!;

[HttpPost]
public async Task<ProductDto> Create(ProductDto dto, CancellationToken cancellationToken)
{
    // Sanitize HTML to prevent XSS attacks
    dto.DescriptionHTML = htmlSanitizer.Sanitize(dto.DescriptionHTML ?? string.Empty);

    var entityToAdd = dto.Map();
    // ... rest of create logic
}
```

---

## Proxy Interface Pattern

This project uses a **strongly-typed HTTP client wrapper** pattern to call backend APIs. This provides type safety, IntelliSense support, and eliminates magic strings.

### How It Works

The pattern involves:
1. Define an interface in `Shared/Controllers` (shared between client and server)
2. Implement the interface in `Boilerplate.Server.Api/Controllers` (server-side)
3. Client code injects the interface and calls methods as if they were local

### Step 1: Define the Interface

**File**: [`src/Shared/Controllers/Categories/ICategoryController.cs`](/src/Shared/Controllers/Categories/ICategoryController.cs)

```csharp
[Route("api/[controller]/[action]/")]
[AuthorizedApi] // Requires authentication
public interface ICategoryController : IAppController
{
    [HttpGet("{id}")]
    Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken);

    [HttpGet]
    Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken) => default!;

    [HttpGet]
    Task<List<CategoryDto>> Get(CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken);

    [HttpPut]
    Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken);

    [HttpDelete("{id}/{concurrencyStamp}")]
    Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken);
}
```

### Step 2: Implement on Server

**File**: [`src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

```csharp
[ApiController, Route("api/[controller]/[action]")]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    [HttpGet("{id}")]
    public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
    {
        // Implementation
    }

    [HttpGet]
    public async Task<PagedResult<CategoryDto>> GetCategories(
        ODataQueryOptions<CategoryDto> odataQuery, // Server-specific parameter
        CancellationToken cancellationToken)
    {
        // Implementation
    }
    
    // ... other implementations
}
```

### Step 3: Use in Client Code

```csharp
public partial class ProductsPage
{
    [AutoInject] IProductController productController = default!;

    private async Task LoadProducts()
    {
        // Type-safe API call with IntelliSense
        var products = await productController.Get(CurrentCancellationToken);
        
        // Build OData query
        var query = new ODataQuery
        {
            Top = 10,
            Filter = "Price gt 50",
            OrderBy = "Name"
        };
        
        // Apply query and call API
        var pagedResult = await productController
            .WithQuery(query.ToString())
            .GetProducts(CurrentCancellationToken);
    }
}
```

### Convention Over Configuration

The proxy generator follows these conventions:

- **URL Convention**: Method `GetCurrentUser` in `IUserController` → `api/User/GetCurrentUser`
- **HTTP Method**: Use `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` attributes
- **Query Parameters**: Automatically added to URL query string
- **Body Parameters**: Complex types are sent as JSON in request body

### Handling Server-Only Parameters

Some server methods have parameters that don't exist on the client (like `ODataQueryOptions`).

**Solution**: Use `=> default!` in the interface:

```csharp
public interface ICategoryController : IAppController
{
    // Server accepts ODataQueryOptions, but client doesn't pass it
    [HttpGet]
    Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken) => default!;
}
```

The `=> default!` tells the C# compiler this method has a default implementation, preventing build errors.

### Benefits of This Pattern

1. **Type Safety**: Compile-time checking of API calls
2. **IntelliSense**: Auto-completion for API methods and parameters
3. **Refactoring**: Rename a method, and all usages update automatically
4. **No Magic Strings**: No more `HttpClient.GetAsync("/api/categories/get")`
5. **Contract Enforcement**: Interface ensures client and server stay in sync

### Advanced Example: External API Calls

You can also call external APIs using this pattern:

**File**: [`src/Shared/Controllers/Statistics/IStatisticsController.cs`](/src/Shared/Controllers/Statistics/IStatisticsController.cs) (Example)

```csharp
public interface IStatisticsController : IAppController
{
    // Call GitHub API directly
    [HttpGet]
    [ExternalApiUrl("https://api.github.com/repos/bitfoundation/bitplatform")]
    Task<JsonElement> GetGitHubStats(CancellationToken cancellationToken);
}
```

### More Information

For comprehensive details about the proxy interface pattern, see:

**File**: [`src/Shared/Controllers/Readme.md`](/src/Shared/Controllers/Readme.md)

---

## Architectural Philosophy

Before we conclude, it's important to understand the architectural decisions behind this template.

### Backend Architecture: Intentionally Simple

The backend architecture in this template (feature-based controllers directly accessing `DbContext`) is **intentionally kept simple** to help developers get started quickly.

**Key Points**:
- This is NOT the "one true way" to structure a backend
- Different projects have different requirements
- Experienced developers likely have their own architectural preferences
- You are free to implement any architecture you prefer

### Architectural Options

Whether to use:
- **Layered architecture** (Presentation → Business → Data layers)
- **CQRS** (Command Query Responsibility Segregation)
- **Onion architecture** (Domain at the core)
- **Clean architecture**
- **Vertical slice architecture**
- **Domain-Driven Design**

...is entirely up to you and depends on your project requirements and team preferences.

### No Universal Solution

There is **no "one-size-fits-all" architecture** that works for every project:
- A small startup app has different needs than an enterprise system
- A read-heavy system needs different patterns than a write-heavy system
- Team size, experience, and preferences matter

### The Real Value: Frontend Architecture

**The real architectural value of this template is in the frontend**: a complete, production-ready architecture for **cross-platform Blazor applications**.

This is where the template shines, as dotnet frontend architecture patterns are less established in the .NET ecosystem.

### Backend Features Still Included

While the architecture is simple, the backend still includes many advanced features:
- ✅ Full-featured ASP.NET Core Identity solution
- ✅ JWT-based authentication with refresh tokens
- ✅ AI integration (semantic search, chatbots)
- ✅ Super-optimized response caching system
- ✅ Background job processing with Hangfire
- ✅ SignalR real-time communication
- ✅ OData query support
- ✅ Health checks and diagnostics
- ✅ OpenTelemetry integration
- ✅ Push notifications
- ✅ Localization and multi-language support
- ✅ And many more...

### Bottom Line

Feel free to restructure the backend however you see fit. The template provides a solid foundation and advanced features, but you're in control of the architecture.

---