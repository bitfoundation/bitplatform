# Scaffold Complete Entity with Full CRUD

You are an expert at scaffolding complete entity implementations for the project.

## Instructions

Generate a complete CRUD implementation for an entity including:
1. **Entity Model**
2. **Entity Type Configuration** (Entity Framework Core)
3. **DbContext Registration** (DbSet)
4. **EF Core Migration**
5. **DTO** (Data Transfer Object)
6. **Mapper** (using Mapperly)
7. **API Controller**
8. **IAppController Interface** (Strongly-typed HTTP client)
9. **Resource Strings** (AppStrings.resx)
10. **Data Grid Page**
11. **Add/Edit Page**
12. **PageUrls.cs**, **NavBar.razor** and **MainLayout.razor.items.cs** integration

### Entity (Model)
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **File**: `{EntityName}.cs`
- **Requirements**:
  - Include `Id`, `Version` properties
  - Add appropriate navigation properties
  - Use nullable reference types
  - Add data annotations as needed

### Entity Configuration, AppDbContext DbSet and Migration
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **Files**:
  - `{EntityName}Configuration.cs` - Implement `IEntityTypeConfiguration<{EntityName}>`
  - Configure unique indexes and relationships
  - Automatically registered in `AppDbContext` via `modelBuilder.ApplyConfigurationsFromAssembly()`
- **Migration**: 
  - Run: `dotnet ef migrations add {MigrationName} --output-dir Infrastructure/Data/Migrations --verbose` in `Boilerplate.Server.Api` project

### DTO
- **Location**: `src/Shared/Boilerplate.Shared/Features/{FeatureName}/`
- **File**: `{EntityName}Dto.cs`
- **Requirements**:
  - Use `[DtoResourceType(typeof(AppStrings))]` attribute
  - Add validation attributes: `[Required]`, `[MaxLength]`, `[Display]`
  - Use `nameof(AppStrings.PropertyName)` for error messages and display names
  - Include `Id`, `Version` properties
  - Add calculated properties if needed (e.g., `ProductsCount`)
  - Add `[JsonSerializable(typeof({DtoName}))]` to `AppJsonContext.cs`

### Mapper
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **File**: `{EntityName}Mapper.cs` (or `{FeatureName}Mapper.cs` if multiple entities)
- **Requirements**:
  - Use `[Mapper]` attribute from Mapperly
  - Create `static partial class {MapperName}Mapper`
  - Add projection method: `public static partial IQueryable<{DtoName}> Project(this IQueryable<{EntityName}> query);`
  - Add mapping methods: `Map()`, `Patch()` for CRUD operations
  - Use `[MapProperty]` for complex mappings if needed

### API Controller
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **File**: `{EntityName}Controller.cs`
- **Requirements**:
  - Inherit from `AppControllerBase`
  - Implement the corresponding `IAppController` interface
  - Add appropriate authorization attributes
  - Use `[EnableQuery]` for GET endpoints with OData support
  - Implement validation in private methods
  - Use `Project()` for querying and mapping
  - Handle resource not found scenarios using `ResourceNotFoundException`

### IAppController Interface
- **Location**: `src/Shared/Boilerplate.Shared/Features/{FeatureName}/`
- **File**: `I{EntityName}Controller.cs`
- **Requirements**:
  - Inherit from `IAppController`
  - Add `[Route("api/[controller]/[action]/")]` attribute
  - Add `[AuthorizedApi]` if authentication required
  - Always use `CancellationToken` parameters
  - The return type should be `Task<T>` or `Task<T>` where T is JSON Serializable type like DTO, int, or List<Dto>
  - If Backend API's action returns `IQueryable<T>`, use `Task<List<T>>` as return type with `=> default!`
  - If Backend API's action returns `IActionResult`, use `Produces<T>` attribute to specify the response type with `=> default!`
  - If Backend API accepts `ODataQueryOptions`, simply ignore it

### Pages

Every Blazor page follows a three-file structure:
- `PageName.razor` - UI markup with Razor syntax
- `PageName.razor.cs` - Code-behind with C# logic
- `PageName.razor.scss` - Scoped styles

**Location**: `src/Client/Boilerplate.Client.Core/Components/Pages/{FeatureName}/`

- **Grid/List Page**: `{FeatureName}Page.razor` + `.razor.cs` + `.razor.scss`
- **Add/Edit Modal or Page**: `AddOrEdit{EntityName}Page.razor` or `AddOrEdit{EntityName}Modal.razor`

Use SCSS variables from `_bit-css-variables.scss` for theming:
```scss
@import '../../Styles/abstracts/_bit-css-variables.scss';
background: $bit-color-background-secondary;
color: $bit-color-primary;
```

Always use `WrapHandled` for event handlers and lifecycle methods. Exceptions are caught and handled by `ExceptionHandler`:
```razor
<BitButton OnClick="WrapHandled(SaveData)" />
<BitTextField OnEnter="WrapHandled(async (args) => await Submit())" />
```