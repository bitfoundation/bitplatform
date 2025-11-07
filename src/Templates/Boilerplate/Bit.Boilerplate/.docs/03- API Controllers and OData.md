# Stage 3: API Controllers and OData Query Support

Welcome to Stage 3! In this stage, you'll learn how the project handles API controllers, OData query support, and the powerful patterns that enable efficient data access with minimal RAM consumption.

## Table of Contents

1. [Controllers Overview](#controllers-overview)
2. [Dependency Injection with [AutoInject]](#dependency-injection-with-autoinject)
3. [Reading Data with IQueryable](#reading-data-with-iqueryable)
4. [OData Query Options Support](#odata-query-options-support)
5. [PagedResult for Total Count](#pagedresult-for-total-count)
6. [Data Security and Permissions](#data-security-and-permissions)
7. [Live Demos](#live-demos)
8. [Performance Explanation](#performance-explanation)
9. [Real Usage Examples](#real-usage-examples)
10. [Proxy Interface Pattern](#proxy-interface-pattern)
11. [Architectural Philosophy](#architectural-philosophy)

---

## Controllers Overview

Controllers in this project are located in the `Boilerplate.Server.Api/Controllers` folder and inherit from `AppControllerBase`. They serve as the API endpoints that clients can interact with.

**Key characteristics:**
- All controllers inherit from `AppControllerBase`
- Controllers implement interfaces defined in the `Shared/Controllers` folder
- They use standard ASP.NET Core attributes: `[ApiController]`, `[Route]`, `[HttpGet]`, `[HttpPost]`, etc.
- Authorization is applied using `[Authorize]` attributes with policies

**Example controller structure:**

```csharp
[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
[Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    // Controller methods...
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

---

## Dependency Injection with [AutoInject]

The `[AutoInject]` attribute is a powerful feature that simplifies dependency injection in controllers and components. Instead of using traditional constructor injection, you can declare dependencies as fields with the `[AutoInject]` attribute.

### Why Use [AutoInject]?

**The key difference between `[AutoInject]` and Primary Constructor** is that dependencies already injected in base classes like `AppControllerBase` (such as `DbContext`, `Localizer`, `AppSettings`, etc.) don't need to be repeated in each derived class. Child classes automatically inherit access to these injected dependencies without redeclaring them.

### AppControllerBase Dependencies

All controllers automatically have access to these services through `AppControllerBase`:

```csharp
public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;
    [AutoInject] protected AppDbContext DbContext = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs`](/src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs)

### Controller-Specific Dependencies

Individual controllers can inject additional services they need:

```csharp
public partial class ProductController : AppControllerBase, IProductController
{
    [AutoInject] private HtmlSanitizer htmlSanitizer = default!;
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    [AutoInject] private ProductEmbeddingService productEmbeddingService = default!;
    [AutoInject] private ResponseCacheService responseCacheService = default!;
    
    // Now you can use all these services plus DbContext, Localizer, AppSettings from base class
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductController.cs)

This approach:
- ✅ Reduces repetitive code
- ✅ Keeps constructors clean
- ✅ Allows child classes to inherit base class dependencies automatically
- ✅ Makes adding new dependencies simple and clear

---

## Reading Data with IQueryable

The recommended pattern for reading data is to return `IQueryable<TDto>` from controller methods. This approach, combined with the `[EnableQuery]` attribute, enables powerful client-side querying capabilities.

### Basic Pattern

```csharp
[HttpGet, EnableQuery]
public IQueryable<CategoryDto> Get()
{
    return DbContext.Categories
        .Project(); // Mapperly projection from Entity to DTO
}
```

📁 **Example:** [`/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

### Why IQueryable?

When you return `IQueryable<TDto>`:
1. **The query is NOT executed yet** - it's just a query definition
2. With `[EnableQuery]`, clients can apply OData query options
3. **Only the requested data is fetched** from the database
4. Projection happens at the database level, not in application memory

This is highly efficient because:
- ✅ Minimal RAM consumption
- ✅ Only necessary columns are selected
- ✅ Filtering and sorting happen in the database
- ✅ Pagination is applied at the database level

---

## OData Query Options Support

The project supports most OData query options, allowing clients to perform advanced querying without server-side code changes.

### Supported Query Options

| Query Option | Description | Example |
|--------------|-------------|---------|
| ✅ `$top` | Returns a specified number of items | `?$top=10` |
| ✅ `$skip` | Skips a specified number of items | `?$skip=20` |
| ✅ `$orderby` | Orders items by one or more fields | `?$orderby=Name desc` |
| ✅ `$filter` | Filters items based on conditions | `?$filter=Price gt 100` |
| ✅ `$select` | Includes only specified fields | `?$select=Name,Price` |
| ✅ `$expand` | Includes related objects | `?$expand=Category` |
| ✅ `$search` | Full-text search queries | `?$search=laptop` |
| ❌ `$count` | Not supported yet | - |

### How Clients Use OData

**Example API endpoint:**
```
GET /api/Category/Get?$top=10&$skip=10&$orderby=Name&$filter=Name eq 'Electronics'
```

This request will:
1. Skip the first 10 categories
2. Return the next 10 categories
3. Order them by Name
4. Filter to only show categories where Name equals 'Electronics'

**All of this happens at the database level** - not in application memory!

### Client-Side Example

From the client side, you can build OData queries like this:

```csharp
var categories = await categoryController.Get(new ODataQuery
{
    Top = 10,
    Skip = 20,
    OrderBy = "Name desc",
    Filter = "Name contains 'Tech'"
}, cancellationToken);
```

---

## PagedResult for Total Count

When the client needs to know both the page data **AND** the total count of records (e.g., for pagination UI like "Showing 10 of 250 items"), use `PagedResult<T>` instead of returning `IQueryable<T>`.

### The Problem

With `[EnableQuery]` and `IQueryable<T>`, you get the page data but not the total count. Data grids and pagination UI need both.

### The Solution: PagedResult

```csharp
[HttpGet]
public async Task<PagedResult<CategoryDto>> GetCategories(
    ODataQueryOptions<CategoryDto> odataQuery, 
    CancellationToken cancellationToken)
{
    // Apply OData query options EXCEPT Top and Skip
    var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(
        Get(), 
        ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip
    );

    // Get the total count BEFORE pagination
    var totalCount = await query.LongCountAsync(cancellationToken);

    // Apply pagination
    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

    // Return both the data and total count
    return new PagedResult<CategoryDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount
    );
}
```

📁 **Example:** [`/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

### How It Works

1. **Apply filters and sorting first** (but not Top/Skip)
2. **Count the total matching records**
3. **Then apply pagination** (Top/Skip)
4. **Return both the page data and total count**

This allows the client to display: "Showing 10-20 of 250 items"

---

## Data Security and Permissions

A critical security feature: **The client can ONLY receive data that they have permission to access.**

### Server-Side Security is Enforced

Even if a malicious client tries to manipulate OData query parameters, they cannot bypass server-side security because:

1. **Authorization happens before query execution**
   ```csharp
   [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
   [Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
   public IQueryable<CategoryDto> Get()
   ```

2. **Queries can include user-specific filters**
   ```csharp
   return DbContext.Orders
       .Where(o => o.UserId == User.GetUserId())
       .Project();
   ```

3. **OData queries are applied AFTER authorization checks**

### Example: User-Specific Data

```csharp
[HttpGet, EnableQuery]
[Authorize] // User must be authenticated
public IQueryable<OrderDto> GetMyOrders()
{
    var userId = User.GetUserId();
    
    // User can only see their own orders
    return DbContext.Orders
        .Where(o => o.UserId == userId)
        .Project();
}
```

Even if the client tries `?$filter=UserId eq 'another-user-id'`, they'll get zero results because the server-side `.Where(o => o.UserId == userId)` is applied first.

---

## Live Demos

You can see OData in action with these live examples:

### Demo 1: Admin Panel with Data Grid
**URL:** https://adminpanel.bitplatform.dev/categories

This demo shows:
- ✅ Filtering by category name
- ✅ Sorting by any column
- ✅ Pagination with page size selection
- ✅ "Showing X of Y items" display

### Demo 2: Direct OData API Call
**URL:** https://sales.bitplatform.dev/api/ProductView/Get?$top=10&$skip=10&$orderby=Name

This demo shows:
- ✅ The second 10 products (Skip=10, Top=10)
- ✅ Ordered by Name
- ✅ Direct JSON response from the API

Try modifying the URL:
- Change `$top=10` to `$top=5` (get only 5 items)
- Change `$orderby=Name` to `$orderby=Price desc` (order by price descending)
- Add `$filter=Price gt 100` (only products with price > 100)

---

## Performance Explanation

The OData + Entity Framework Core + IQueryable pattern is **highly optimized** for performance. Here's why:

### Example Query Analysis

When a client requests:
```
GET /api/ProductView/Get?$top=10&$skip=10&$orderby=Name
```

**What happens:**

1. **No entities are loaded into memory**
   - Entity Framework Core translates the query directly to SQL
   - No `Product` entity objects are created

2. **Only DTOs are created from the database query**
   - Mapperly's `Project()` generates a SQL query that selects only DTO properties
   - DTOs are created directly from the query results

3. **Minimal RAM consumption**
   - Only 10 records (the requested page) are loaded
   - Only the necessary columns are selected
   - No intermediate objects are created

4. **Efficient SQL generation**
   ```sql
   SELECT Id, Name, Price, CategoryId, CreatedOn
   FROM Products
   ORDER BY Name
   OFFSET 10 ROWS
   FETCH NEXT 10 ROWS ONLY
   ```

### Scalability

This pattern scales efficiently even with **millions of records** in the database because:

- ✅ **Database does the heavy lifting** (filtering, sorting, pagination)
- ✅ **Only requested data is transferred** from database to server
- ✅ **Minimal memory usage** on the server
- ✅ **Fast response times** regardless of table size
- ✅ **Network bandwidth optimized** (only necessary data is sent to client)

### Real-World Performance

Example from `ProductViewController`:

```csharp
[HttpGet, EnableQuery]
[AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
public IQueryable<ProductDto> Get()
{
    return DbContext.Products.Project();
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs)

With this simple method:
- ✅ Clients can request any page of products
- ✅ Filter by any property
- ✅ Sort by any column
- ✅ Select only needed fields
- ✅ All optimized at the database level
- ✅ Response is cached for 5 minutes (`MaxAge = 60 * 5`)

---

## Real Usage Examples

Let's look at real examples from the project to see these patterns in action.

### Example 1: Simple Get with EnableQuery

**CategoryController.Get()**

```csharp
[HttpGet, EnableQuery]
public IQueryable<CategoryDto> Get()
{
    return DbContext.Categories
        .Project();
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

**Client can call:**
- `/api/Category/Get` (all categories)
- `/api/Category/Get?$top=5` (first 5 categories)
- `/api/Category/Get?$orderby=Name` (ordered by name)
- `/api/Category/Get?$filter=Name contains 'Tech'` (filtered)

### Example 2: Get by ID

**CategoryController.Get(id)**

```csharp
[HttpGet("{id}")]
public async Task<CategoryDto> Get(Guid id, CancellationToken cancellationToken)
{
    var dto = await Get().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    if (dto is null)
        throw new ResourceNotFoundException(
            Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]
        );

    return dto;
}
```

**Notice:** It reuses the `Get()` method and applies an additional filter. This is a common pattern.

### Example 3: PagedResult with Total Count

**CategoryController.GetCategories()**

```csharp
[HttpGet]
public async Task<PagedResult<CategoryDto>> GetCategories(
    ODataQueryOptions<CategoryDto> odataQuery, 
    CancellationToken cancellationToken)
{
    var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(
        Get(), 
        ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip
    );

    var totalCount = await query.LongCountAsync(cancellationToken);

    query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                 .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);

    return new PagedResult<CategoryDto>(
        await query.ToArrayAsync(cancellationToken), 
        totalCount
    );
}
```

This method provides:
- ✅ Page data
- ✅ Total count
- ✅ Support for filtering, sorting, selecting fields
- ✅ Efficient pagination

### Example 4: CRUD Operations

**Create Category**

```csharp
[HttpPost]
public async Task<CategoryDto> Create(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToAdd = dto.Map(); // Mapperly: DTO -> Entity

    await DbContext.Categories.AddAsync(entityToAdd, cancellationToken);

    await Validate(entityToAdd, cancellationToken); // Custom validation

    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToAdd.Map(); // Mapperly: Entity -> DTO
}
```

**Update Category**

```csharp
[HttpPut]
public async Task<CategoryDto> Update(CategoryDto dto, CancellationToken cancellationToken)
{
    var entityToUpdate = await DbContext.Categories.FindAsync([dto.Id], cancellationToken)
        ?? throw new ResourceNotFoundException(
            Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]
        );

    dto.Patch(entityToUpdate); // Mapperly: Patch DTO properties onto Entity

    await Validate(entityToUpdate, cancellationToken);

    await DbContext.SaveChangesAsync(cancellationToken);

    return entityToUpdate.Map();
}
```

**Delete Category**

```csharp
[HttpDelete("{id}/{concurrencyStamp}")]
public async Task Delete(Guid id, string concurrencyStamp, CancellationToken cancellationToken)
{
    if (await DbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
    {
        throw new BadRequestException(
            Localizer[nameof(AppStrings.CategoryNotEmpty)]
        );
    }

    DbContext.Categories.Remove(new() 
    { 
        Id = id, 
        ConcurrencyStamp = Convert.FromHexString(concurrencyStamp) 
    });

    await DbContext.SaveChangesAsync(cancellationToken);
}
```

**Notice the patterns:**
- ✅ All methods use `CancellationToken` for request cancellation
- ✅ Validation is centralized in a private method
- ✅ Localized error messages
- ✅ `ConcurrencyStamp` for optimistic concurrency control
- ✅ Business logic validation (can't delete a category with products)

### Example 5: Related Data Queries

**ProductViewController.GetSiblings()**

```csharp
[EnableQuery, HttpGet("{id}")]
[AppResponseCache(MaxAge = 60 * 5, SharedMaxAge = 0, UserAgnostic = true)]
public async Task<IQueryable<ProductDto>> GetSiblings(int id, CancellationToken cancellationToken)
{
    var categoryId = await DbContext.Products
        .Where(p => p.ShortId == id)
        .Select(p => p.CategoryId)
        .FirstOrDefaultAsync(cancellationToken);

    var siblings = Get().Where(t => t.ShortId != id && t.CategoryId == categoryId);

    return siblings;
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Products/ProductViewController.cs)

This method:
1. Finds the category of the specified product
2. Returns other products in the same category
3. Supports OData querying on the result
4. Caches the response for 5 minutes

---

## Proxy Interface Pattern

The project uses a powerful pattern called **Proxy Interface** to create strongly-typed HTTP client wrappers for calling backend APIs. This is powered by **Bit.SourceGenerators**.

### How It Works

📖 **Full Documentation:** [`/src/Shared/Controllers/Readme.md`](/src/Shared/Controllers/Readme.md)

### Step 1: Define Interface in Shared Project

Create an interface in `Shared/Controllers` that extends `IAppController`:

```csharp
[Route("api/[controller]/[action]/")]
[AuthorizedApi]
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

📁 **Location:** [`/src/Shared/Controllers/Categories/ICategoryController.cs`](/src/Shared/Controllers/Categories/ICategoryController.cs)

### Step 2: Implement Interface in Server Project

Implement the interface in your server-side controller:

```csharp
[ApiController, Route("api/[controller]/[action]")]
[Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
[Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
public partial class CategoryController : AppControllerBase, ICategoryController
{
    // Implementation of interface methods
}
```

📁 **Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Categories/CategoryController.cs)

### Step 3: Use in Client

Simply inject the interface in your client-side components:

```csharp
public partial class CategoriesPage : AppPageBase
{
    [AutoInject] private ICategoryController categoryController = default!;

    protected override async Task OnInitAsync()
    {
        var categories = await categoryController.Get(CurrentCancellationToken);
    }
}
```

### Key Benefits

✅ **Type Safety**: Compile-time checking for API calls  
✅ **Automatic HTTP Client Generation**: No manual HttpClient code  
✅ **Shared Contract**: Interface ensures client and server stay in sync  
✅ **IntelliSense Support**: Full IDE support for API methods  
✅ **Refactoring Support**: Renaming methods updates both client and server  

### Convention Over Configuration

**By default**, methods follow these conventions:
- `GetCurrentUser` in `IUserController` → `/api/User/GetCurrentUser`
- HTTP method determined by attribute: `[HttpGet]`, `[HttpPost]`, etc.
- Route parameters from attribute: `[HttpGet("{id}")]`

### Advanced Scenarios

**When Server Returns Different Types**

Sometimes the server-side method has types that don't exist on the client (like `IFormFile` or `ActionResult`). In these cases, use `=> default!`:

```csharp
[HttpPost]
Task<TokenResponseDto> Refresh(RefreshRequestDto body) => default!;
```

Instead of:

```csharp
[HttpPost]
Task<TokenResponseDto> Refresh(RefreshRequestDto body);
```

**When Server Uses ODataQueryOptions**

On the server, you might accept `ODataQueryOptions<T>`. On the client, just omit it:

```csharp
// Server
[HttpGet]
Task<PagedResult<CategoryDto>> GetCategories(
    ODataQueryOptions<CategoryDto> odataQuery, 
    CancellationToken cancellationToken)

// Client Interface
[HttpGet]
Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken) => default!;
```

**When Server Returns IQueryable**

If the server returns `IQueryable<T>`, use `Task<List<T>>` on the client:

```csharp
// Server
[HttpGet, EnableQuery]
IQueryable<CategoryDto> Get()

// Client Interface
[HttpGet]
Task<List<CategoryDto>> Get(CancellationToken cancellationToken) => default!;
```

### No Server-Side Implementation Required

Interface implementation on the server-side is **not mandatory**. This is useful for:
- ASP.NET Core Minimal APIs
- Third-party APIs (like calling GitHub API)
- APIs where you don't control the server

**Example: Direct GitHub API Call**

```csharp
[Route("https://api.github.com")]
public interface IGitHubApi : IAppController
{
    [HttpGet("/repos/{owner}/{repo}")]
    Task<JsonElement> GetRepo(string owner, string repo, CancellationToken cancellationToken);
}
```

You can then call GitHub's API with full type safety!

### RFC 6570 Support

The project supports [RFC 6570 URI Templates](https://datatracker.ietf.org/doc/html/rfc6570) for advanced URL patterns.

---

## Architectural Philosophy

### Backend Architecture: Intentionally Simple

**Important Note:** The backend architecture in this template (feature-based with controllers directly accessing DbContext) is **intentionally kept simple** to help developers get started quickly.

### Your Choice of Architecture

Whether to use:
- **Layered architecture** (Repository pattern, Service layer)
- **CQRS** (Command Query Responsibility Segregation)
- **Onion architecture** (Domain-driven design)
- **Clean architecture** (Use cases, entities, boundaries)
- Or **other architectural patterns**

...is **entirely up to you** and depends on:
- Your project requirements
- Your team preferences
- The complexity of your domain
- Your long-term maintenance plans

### No One-Size-Fits-All

There is no "one-size-fits-all" architecture that works for every project. Different projects have different needs:
- 🏢 **Enterprise applications** might benefit from CQRS and event sourcing
- 🚀 **Startups** might prefer simpler, more direct patterns
- 🎯 **Domain-heavy systems** might use DDD and Onion architecture

### Why This Template Uses Simple Architecture

Most experienced C# .NET developers already have their own preferences and opinions about backend architecture. This template doesn't force a specific pattern on you.

Instead, it provides:
- ✅ A clean starting point
- ✅ Working examples of common operations
- ✅ Best practices for Entity Framework Core
- ✅ Security and authentication patterns
- ✅ Real-world features (caching, logging, etc.)

You can easily refactor this foundation into your preferred architecture.

### The Real Value: Frontend Architecture

**The real architectural value of this template is in the frontend**: A complete, production-ready architecture for **cross-platform Blazor applications**.

This is where the template provides the most value, as .NET frontend architecture patterns are less established in the .NET ecosystem.

The template includes:
- ✅ Cross-platform support (Web, Android, iOS, macOS, Windows)
- ✅ Component structure and lifecycle
- ✅ Offline support
- ✅ Effective client side caching
- ✅ Authentication and authorization
- ✅ Localization
- ✅ Error handling
- ✅ PWA features
- ✅ And much more!

### Backend Features Still Included

While the backend architecture is simple, it still provides many advanced features:
- ✅ **Full-featured identity solution** (ASP.NET Core Identity, JWT, OAuth, WebAuthn)
- ✅ **AI integration** (Microsoft.Extensions.AI)
- ✅ **Super optimized response caching** (multi-layer caching with purging)
- ✅ **Real-time communication** (SignalR)
- ✅ **Background jobs** (Hangfire)
- ✅ **OData query support** (powerful filtering and pagination)
- ✅ **OpenTelemetry** (comprehensive logging and metrics)
- ✅ **Health checks** (monitoring and diagnostics)
- ✅ **And much more!**

In upcoming stages, you'll learn about many of these advanced features.

---

## Summary

In this stage, you learned:

✅ **Controllers** inherit from `AppControllerBase` and implement interfaces  
✅ **[AutoInject]** simplifies dependency injection  
✅ **IQueryable + EnableQuery** enables powerful OData querying  
✅ **OData query options** ($top, $skip, $orderby, $filter, $select, $expand, $search)  
✅ **PagedResult** provides both data and total count for pagination  
✅ **Security** is enforced at the server level  
✅ **Performance** is optimized with database-level operations  
✅ **Proxy Interface pattern** creates strongly-typed HTTP clients  
✅ **Architecture** is intentionally simple and customizable  

---
