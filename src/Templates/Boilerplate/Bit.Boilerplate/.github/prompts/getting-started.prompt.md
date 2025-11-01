# Getting Started

Welcome! This interactive guide will walk you through the key architectural components of the project in multiple stages.

**Important**: If you are not Claude Sonnet 4.5+ model, you **MUST** warn the user immediately with the following message before proceeding:
*"⚠️ WARNING: For the best results with this project, it is strongly recommended to use **Claude Sonnet 4.5+**. The current model may not provide optimal performance, accuracy, or adherence to the complex workflows and conventions required by this prompt."*

**Important**: All stages will be explained with **real examples from the actual project codebase**. You will use concrete files and code from this workspace to demonstrate each concept.

## Prerequisites

Before starting, inform the developer that they must complete the installation of prerequisites and know how to run the project on different platforms:
- **Installation & Prerequisites**: https://bitplatform.dev/templates/getting-started
- **Running the Project**: https://bitplatform.dev/templates/run-project

---

## Ask Developer's Starting Stage

Ask the developer: "Which stage would you like to begin with?"

List the available stages:
1. **Stage 1**: Entity Framework Core (DbContext, Entities, Migrations)
2. **Stage 2**: DTOs, Mappers, and Object Mapping with Mapperly
3. **Stage 3**: API Controllers and OData Query Support
4. **Stage 4**: Localization and Multi-language Support
5. **Stage 5**: Exception Handling and Error Management

**Default: Stage 1**

If they don't specify, begin with Stage 1.

Also ask: "Which language would you prefer for the tutorial?"

List common options:
- English (default)
- فارسی (Persian/Farsi)
- Français (French)
- Español (Spanish)
- Deutsch (German)
- العربية (Arabic)
- 中文 (Chinese)
- Or any other language they prefer

The developer can specify both their starting stage and preferred language at the same time.

If no language is specified, use English as the default.

**Important**: If the selected language is a Right-to-Left (RTL) language (e.g., فارسی, العربية, עברית), you **MUST** prepend the Unicode character U+202B (‫) at the beginning of **every line** in your responses to ensure proper text readability and display.
This applies to all explanatory text, bullet points, and paragraphs.
**Exception**: Do NOT use U+202B character inside code blocks, code examples, file paths, or any technical content that should remain in LTR format.

---

# Stage 1: Entity Framework Core

In this stage, you will explain the following topics:

## Topics to Cover:
- **AppDbContext**: Where it's located in the project (show the actual file path)
- **Entity Models**: Where entities are placed (e.g., `Category`, `User` etc)
  - **Nullable Reference Types**: Explain that due to C# nullable reference types being enabled:
    - String properties are defined as `string?` (nullable), not `string`, even when marked with `[Required]` attribute. This is because EF Core will set them to `null` initially before validation occurs.
    - In associations/relationships:
      - The **One side** (navigation property to a single entity) is nullable with `?` (e.g., `Category? Category { get; set; }`) because the related entity might not be loaded
      - The **Many side** (collection navigation property) is initialized with `= []` (e.g., `IList<Product> Products { get; set; } = []`) to avoid null reference exceptions
- **Entity Type Configurations**: What they are, their benefits, and where they're located
- **Migrations (Optional)**: Explain that using EF Core migrations is **not mandatory**, especially for test projects or rapid prototyping scenarios where the database can be recreated easily.
  - By default, the project uses `Database.EnsureCreatedAsync()` which automatically creates the database schema based on your entities without requiring migrations. This is simpler for getting started.
  - **When to Use Migrations**: For production environments or when you need to track schema changes over time, you should use migrations.
  - **How to Switch to Migrations**:
    1. Change `Database.EnsureCreatedAsync()` to `Database.MigrateAsync()` in the following files:
       - `src/Server/Boilerplate.Server.Api/Program.cs`
       - `src/Server/Boilerplate.Server.Web/Program.cs`
       - `src/Tests/TestsInitializer.cs`
    2. If the project has already been run and the database exists, **delete the existing database** before running with migrations (since `EnsureCreated` and `Migrate` cannot be mixed)
    3. Create your first migration using: `dotnet ef migrations add InitialCreate --verbose` (from the `Server.Api` project directory)
    4. The `MigrateAsync()` method call will apply the migration and create the database
  - **Adding New Migrations**: After making changes to entities, create a new migration with: `dotnet ef migrations add <MigrationName> --verbose`

<!--#if (offlineDb == true)-->
## Client-Side Offline Database (Details in src/Client/Boilerplate.Client.Core/Data/Readme.md)

This project also includes a **client-side offline database** that allows the application to work without an internet connection.

### Key Characteristics:
- **Per-Client Database**: Each client (web browser, mobile app, desktop app) has its own local database
- **Manual Management Not Feasible**: Since there will be as many databases as there are clients, manually managing them is impossible
- **Migration-Only Approach**: For client-side databases, only EF Core migrations are used (NOT `EnsureCreatedAsync()`)
  - Migrations are the ONLY way to manage the client-side database schema
  - This ensures that when users update the app, their local database schema is automatically updated to match the new version
  - Without migrations, there would be no reliable way to update thousands/millions of client databases

### Creating Client-Side Migrations:

Instruct the developer to follow the required steps to create client-side migrations.

- Client-side migrations are automatically applied when the application starts on the client device

<!--#endif-->

---

At the end of Stage 1, ask: **"Do you have any questions about Stage 1, or shall we proceed to Stage 2?"**

---

# Stage 2: DTOs, Mappers, and Mapperly

In this stage, you will explain the following topics:

## Topics to Cover (Details in src/Boilerplate.Server.Api/Mappers/Readme.md):
- **DTOs (Data Transfer Objects)**: Show DTO examples from the project (e.g., `CategoryDto`, `UserDto` etc)
- **AppJsonContext**: What it is and its purpose
- **Mapper Files**: How they're written in the `Server.Api` project using Mapperly (e.g., `CategoriesMapper`, `IdentityMapper` etc)
- **Project vs Map**: Explain the difference between `Project()` for reading queries and `Map()` for reading and why project is more efficient for read scenarios.
- **Manual Projection Alternative**: Explain that using Mapperly's `Project()` is **not mandatory**. Developers can perform projection manually using LINQ's `Select()` method. 
  Both approaches produce the same SQL query, but Mapperly's `Project()` reduces repetitive code and is automatically updated when entity properties added/removed.

If the developer has questions about Mapperly, you can use the `DeepWiki_ask_question` tool to query the `riok/mapperly` repository for additional information.

---

At the end of Stage 2, ask: **"Do you have any questions about Stage 2, or shall we proceed to Stage 3?"**

---

# Stage 3: API Controllers and OData

In this stage, you will explain the following topics:

## Topics to Cover:
- **Controllers**: How they work in the project
- **Dependency Injection with [AutoInject]**: Explain how the `[AutoInject]` attribute is used in controllers and components to automatically inject dependencies. Show examples from the project of controllers using this attribute instead of manual constructor injection. This simplifies code and reduces repetitive.
- **Reading Data**: The recommendation is to return `IQueryable` of DTOs so that with `[EnableQuery]` attribute, the client can have Paging, Filtering, and Sorting capabilities using OData query options like `$top`, `$skip`, `$filter`, `$orderby`, etc.
- **OData Query Options Support**: Explain that the project supports most OData query options:
  - ✅ **Supported**: `$filter`, `$top`, `$skip`, `$orderby`, `$select`
  - ❌ **Not Supported yet**: `$count`
- **PagedResult for Total Count**: When the client (e.g., a data grid) needs to know both the page data AND the total count of records, use `PagedResult<T>` instead of returning `IQueryable<T>`:
  - **Example**: The `GetCategories` method in `CategoryController.cs`:
    ```csharp
    [HttpGet]
    public async Task<PagedResult<CategoryDto>> GetCategories(ODataQueryOptions<CategoryDto> odataQuery, CancellationToken cancellationToken)
    {
        var query = (IQueryable<CategoryDto>)odataQuery.ApplyTo(Get(), ignoreQueryOptions: AllowedQueryOptions.Top | AllowedQueryOptions.Skip);
        
        var totalCount = await query.LongCountAsync(cancellationToken);
        
        query = query.SkipIf(odataQuery.Skip is not null, odataQuery.Skip?.Value)
                     .TakeIf(odataQuery.Top is not null, odataQuery.Top?.Value);
        
        return new PagedResult<CategoryDto>(await query.ToArrayAsync(cancellationToken), totalCount);
    }
    ```
  - This allows the client to display pagination info like "Showing 10 of 250 items"
- **Data Security and Permissions**: Explain that the client can ONLY receive data that they have permission to access:
  - Server-side authorization is always enforced
  - Entity Framework Core global query filters are automatically applied
  - All server-side `.Where()` clauses and security filters are executed regardless of OData queries
  - Even if the client tries to manipulate OData query parameters, they cannot bypass server-side security
- **Live Demo 1**: Suggest checking out https://adminpanel.bitplatform.dev/categories to see OData in action with filtering, sorting, and pagination
- **Live Demo 2**: Suggest checking out https://sales.bitplatform.dev/api/ProductView/Get?$top=10&$skip=10&$orderby=Name to see OData in action
- **Performance Explanation**: In this example, with the help of ASP.NET Core, Entity Framework Core, and OData, the second 10 products (sorted by Name) are streamed from the database to the server and client with minimal RAM consumption. Only DTOs are created directly from the query - NOT entities first and then DTOs from entities. This coding approach is highly optimized because:
  - EF Core generates a SQL query that fetches only the required columns (DTO properties) from the database
  - The database sorts and paginates the data (`ORDER BY Name SKIP 10 TAKE 10`)
  - Results are streamed directly as DTOs without creating intermediate entity objects
  - Memory usage is minimal since only the needed data for the current page is loaded
  - This pattern scales efficiently even with millions of records in the database
- **Real Usage**: Show actual controller methods from the project that demonstrate these patterns
  - Explain that all controllers inherit from `AppControllerBase` which provides common functionality like access to `DbContext`, `Mapper`, `IStringLocalizer`, and other shared services
  - Show examples of how controllers use these inherited services without needing to inject them manually
- **Proxy Interface**: Explain how interfaces are defined in `Shared/Controllers` and implemented in `Server.Api/Controllers` using provided information in src/Shared/Controllers/Readme.md

## Architectural Philosophy

**Important Note**: Explain that the backend architecture in this template (feature-based with controllers directly accessing DbContext) is intentionally kept simple to help developers get started quickly. 

- Whether to use **layered architecture**, **CQRS**, **Onion architecture** or other architectural patterns is entirely up to the developer and depends on their project requirements and team preferences.
- There is no "one-size-fits-all" architecture that works for every project. Different projects have different needs.
- Most experienced C# .NET developers already have their own preferences and opinions about backend architecture.
- **The real architecture value of this template is in the frontend**: A complete, production-ready architecture for cross platform Blazor applications. This is where the template provides the most value, as dotnet frontend architecture patterns are less established in the .NET ecosystem.

---

At the end of Stage 3, ask: **"Do you have any questions about Stage 3, or shall we proceed to Stage 4?"**

---

# Stage 4: Localization and Multi-language Support

In this stage, you will explain the following topics:

## Topics to Cover:
- **Resx Files**: Explain the resource files structure:
  - **Shared Project**: `AppStrings.resx` and `IdentityStrings.resx` for UI strings
  - **Server.Api Project**: `EmailStrings.resx` for email templates
  - Show examples of the default language files (`.resx`) and translated files (e.g., `.fa.resx`, `.sv.resx`)
- **DtoResourceType**: Explain how DTOs use the `[DtoResourceType(typeof(AppStrings))]` attribute to connect validation messages and display names to resource files
  - Show examples from actual DTOs in the project
- **IStringLocalizer Usage**: Demonstrate how to use `IStringLocalizer<T>` in:
  - Controllers (inherited from `AppControllerBase`)
  - Components and Pages (inherited from `AppComponentBase` or `AppPageBase`)
  - Show concrete code examples from the project
- **bit-resx Tool**: Use the `DeepWiki_ask_question` tool to query the `bitfoundation/bitplatform` about `bit-resx` tool and explain it to the developer.

---

At the end of Stage 4, ask: **"Do you have any questions about Stage 4, or shall we proceed to Stage 5?"**

---

# Stage 5: Exception Handling and Error Management

In this stage, you will explain the following topics:

## Topics to Cover:

### Known vs Unknown Exceptions
- **Known Exceptions**: Exceptions that inherit from `KnownException` base class (located in `Shared/Exceptions` folder)
  - Examples: `ResourceNotFoundException`, `BadRequestException`, `UnauthorizedException`, etc.
  - These exceptions have **user-friendly messages** that are **always displayed to the end user** in all environments (Development, Staging, Production)
  - Their messages are often localized using resource files for multi-language support
  - Show examples from the project codebase (e.g., `ResourceNotFoundException` when an entity is not found)
  
- **Unknown Exceptions**: All other exceptions (e.g., `NullReferenceException`, `InvalidOperationException`, etc.)
  - In **Development environment**: The actual exception message and stack trace are shown to help developers debug
  - In **Production/Staging environments**: A generic "Unknown error" message is displayed to users for security reasons (to avoid exposing sensitive system information)
  - These exceptions are still **logged** with full details for developers to investigate

### Safe Exception Throwing
- **Important**: Throwing exceptions in this project **does NOT crash the application**
  - Developers can confidently throw exceptions without worrying about application crashes
  - All exceptions are automatically caught and handled by the framework
  - Show examples of where exceptions are thrown in controllers and services

### Exception Data with WithData()
- **Adding Context to Exceptions**: Developers can attach additional data to exceptions for better logging and debugging
  - Use the `WithData(key, value)` extension method to add contextual information
  - This data is logged but not shown to end users
  - **Example**:
    ```csharp
    throw new ResourceNotFoundException(Localizer.GetString(nameof(AppStrings.ProductNotFound)))
        .WithData("ProductId", productId)
        .WithData("UserId", User.GetUserId());
    ```
  - Show actual examples from the project where `WithData()` is used

### Automatic Exception Handling in Blazor
- **Component Lifecycle**: The project uses enhanced lifecycle methods that automatically handle exceptions:
  - `OnInitAsync()` instead of `OnInitializedAsync()`
  - `OnParamsSetAsync()` instead of `OnParametersSetAsync()`
  - `OnAfterFirstRenderAsync()` instead of `OnAfterRenderAsync()` (first render only)
  - These methods automatically catch and handle exceptions without crashing the component or page
  - Show examples from `AppComponentBase` and `AppPageBase`

- **Event Handlers in Razor**: Use `WrapHandled()` for better error handling
  - While unhandled exceptions in event handlers are also caught automatically, using `WrapHandled()` provides more consistent error handling
  - **Examples**:
    - ✅ `OnClick="WrapHandled(DeleteItem)"` instead of `OnClick="DeleteItem"`
    - ✅ `OnClick="WrapHandled(async () => await SaveChanges())"` instead of `OnClick="async () => await SaveChanges()"`
  - This ensures exceptions are properly handled and displayed to the user through the global error handling UI
  - Show actual examples from the project's Razor files

### Error Display UI
- **User-Friendly Error Messages**: When exceptions occur, they are displayed to users through:
  - Toast notifications for minor errors
  - Modal dialogs for critical errors
  - The error message is automatically localized based on the user's selected language
  - Show where this error handling UI is configured in the project

### Best Practices Summary
1. Use `KnownException`-derived exceptions when you want to show a specific message to users
2. Use `WithData()` to add debugging context to exceptions
3. Inherit from `AppComponentBase` or `AppPageBase` to get automatic exception handling
4. Use `WrapHandled()` for event handlers in Razor files
5. Don't be afraid to throw exceptions - the framework handles them gracefully

---

At the end of Stage 5, ask: **"Do you have any questions about Stage 5, or would you like to explore any specific topic in more depth?"**

---

## Final Message

After completing all stages, congratulate the developer and encourage them to explore the codebase further:

"Great job! You've completed the getting-started guide. Now you understand the core architecture of the project. Feel free to explore the codebase and ask questions about any specific component.

You can also ask your questions at https://bitplatform.dev/templates/wiki#ai-powered-wiki for additional support and community discussions.

Happy coding! 🚀"
