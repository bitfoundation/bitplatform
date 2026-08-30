---
mode: 'agent'
description: Scaffolds complete CRUD entity implementations including entity model, EF configuration, DTO, Mapperly mapper, API controller, IAppController interface, resource strings, Blazor pages, and AppFeatures registration.
---

# Scaffold Complete Entity with Full CRUD

You are an expert at scaffolding complete entity implementations for the project.

## Pre-Implementation Research

**MANDATORY for First-Time CRUD Setup**: Before generating any page files (`.razor`, `.razor.cs`, `.razor.scss`), check the project for existing implementations of `<BitDataGrid`.

* **If a `<BitDataGrid` is already present in the project:** Skip this research step and follow the existing project patterns.
* **If NO `<BitDataGrid` can be found (First CRUD Implementation):** You **MUST** call the bit BlazorUI MCP tools -
  starting with `SearchBitBlazorUI` (describe what each control must **do**) even when its name comes to mind, then
  `GetBitBlazorUIComponent` and `GetBitBlazorUIComponentExamples` for `BitDataGrid`, `BitDialog` and the form
  inputs you need - to retrieve the authoritative component APIs before writing any markup. Do
  **not** use `ask_question` for this: that tool's own description excludes the bit platform libraries, which have
  dedicated tools on the same server.

  There are **two types of CRUD pages** - choose the appropriate one based on the DTO being scaffolded:
  - **Modal Dialog CRUD** - suited for DTOs with a small number of simple properties.
  - **Detailed Page CRUD** - suited for DTOs with many properties, rich text editors, file uploads, or complex forms.

  **Before proceeding, ask the user which mode is appropriate** given the current DTO's structure and requirements.

Use the component documentation returned by those tools as the authoritative reference for all generated Blazor
page files in this initial scaffold.

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
9. **Localized Strings** (see the note under *DTO*)
10. **Data Grid Page**
11. **Add/Edit Modal or Page**
12. **PageUrls.cs**, **NavBar.razor** and **MainLayout.razor.items.cs** integration
13. **AppFeatures.cs** registration

### Entity (Model)
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **File**: `{EntityName}.cs`
- **Requirements**:
  - Include `Id`, `Version` properties
  - Add appropriate navigation properties
  - Use nullable reference types
  - Add data annotations as needed
<!--#if (multitenant == true)-->
  - **Implement `ITenantAware`** unless the entity is deliberately global, or is scoped by something other than the
    tenant. `ITenantAware` is what makes `AppDbContext.ConfigureTenantAwareEntities` attach the
    `HasQueryFilter(x => x.TenantId == CurrentTenantId)` row-level filter and stamp `TenantId` on save; an entity
    that skips it is readable and writable across every tenant. `Product.cs` is the shipped example.
  - `TodoItem` is the counter-example: it is scoped per **user** through `UserId`, not per tenant, so it correctly
    does not implement `ITenantAware`. If you take that route, say so explicitly and put the ownership term in
    every query in the controller - nothing else will add it for you.
<!--#endif-->

### Entity Configuration, AppDbContext DbSet and Migration
- **Location**: `src/Server/Boilerplate.Server.Api/Features/{FeatureName}/`
- **Files**:
  - `{EntityName}Configuration.cs` - Implement `IEntityTypeConfiguration<{EntityName}>`
  - Configure unique indexes and relationships
  - Automatically registered in `AppDbContext` via `modelBuilder.ApplyConfigurationsFromAssembly()`
- **Migration**: 
  - Run: `dotnet ef migrations add {MigrationName} --output-dir Infrastructure/Data/Migrations --verbose` in `Boilerplate.Server.Api` project

### DTO
- **Location**: `src/Shared/Features/{FeatureName}/`
- **File**: `{EntityName}Dto.cs`
- **Requirements**:
  - Use `[DtoResourceType(typeof(AppStrings))]` attribute
  - Add validation attributes: `[Required]`, `[MaxLength]`, `[Display]`
  - Use `nameof(AppStrings.PropertyName)` for error messages and display names. **Validation attributes are the
    one place a new `.resx` key is required**, because `[Display(Name = ...)]` and `[Required(ErrorMessage = ...)]`
    take compile-time constants - `TodoItemDto.cs` is the shipped shape. **Everywhere else** (page markup,
    code-behind, controllers) follow `AGENTS.md` section 5 and write the English text through the
    `IStringLocalizer` indexer, e.g. `Localizer["Category saved."]`; editing `.resx` outside the DTO forces a full
    restart of a running hot-reload session. Moving those literals into `.resx` is a separate, explicitly-requested
    pass - see `.github/prompts/resx.prompt.md`.
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
  - Add `[ApiVersion(1)]` and `[Route("api/v{v:apiVersion}/[controller]/[action]")]`
  - Add appropriate authorization attributes
  - Use `[EnableQuery]` for GET endpoints with OData support
  - Implement validation in private methods
  - Use `Project()` for querying and mapping
  - Handle resource not found scenarios using `ResourceNotFoundException`

### IAppController Interface
- **Location**: `src/Shared/Features/{FeatureName}/`
- **File**: `I{EntityName}Controller.cs`
- **Requirements**:
  - Inherit from `IAppController`
  - Add `[Route("api/v1/[controller]/[action]/")]` attribute, matching every interface the project already ships.
    The interface uses the literal `v1`; the **server controller** uses `[ApiVersion(1)]` together with
    `[Route("api/v{v:apiVersion}/[controller]/[action]")]`. Do not unify the two - the generated typed proxy has
    no `apiVersion` route value to substitute. (An unversioned route also works, since `AddApiVersioning` sets
    `AssumeDefaultVersionWhenUnspecified`, but it would be the only one in the project.)
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

Always use `WrapHandled` for all event handlers. Exceptions are caught and handled by `ExceptionHandler`:
```razor
<BitButton OnClick="WrapHandled(SaveData)" />
<BitTextField OnEnter="WrapHandled(async (args) => await Submit())" />
```
