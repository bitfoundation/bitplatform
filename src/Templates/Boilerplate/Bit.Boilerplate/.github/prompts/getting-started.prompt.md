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
6. **Stage 6**: ASP.NET Core Identity and Authentication
7. **Stage 7**: Blazor Pages, Components, Styling & Navigation
8. **Stage 8**: Dependency Injection & Service Registration
9. **Stage 9**: Configuration (appsettings.json)
10. **Stage 10**: TypeScript, Build Process & JavaScript Interop
11. **Stage 11**: Blazor Modes, PreRendering & PWA
12. **Stage 12**: Force Update System
13. **Stage 13**: Response Caching System
14. **Stage 14**: Logging and OpenTelemetry
15. **Stage 15**: CI/CD Pipeline and Environments
16. **Stage 16**: Automated Testing (Unitigration Tests)
17. **Stage 17**: Other Available Prompt Templates
18. **Stage 18**: Project miscellaneous files
19. **Stage 19**: WebAuthn and Passwordless Authentication (Advanced)

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

**Important**: If the selected language is a Right-to-Left (RTL) language (e.g., فارسی, العربية, עברית), you **MUST** prepend the Unicode character U+202B (‫) at the beginning of **text, bullet points, and paragraphs**, except inside code blocks, code examples, file paths, or any technical content that should remain in LTR format.

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
While backend architecture is simple, it provides lots of features, including but not limited to full featured identity solution, AI integration, super optimized response caching solution etc. In upcoming stages, you will learn about many of these advanced features.

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
- **AppDataAnnotationsValidator**: Explain that this custom validator must be used in Blazor EdtitForms to make `DtoResourceType` work
  - Show examples from the project where `AppDataAnnotationsValidator` is used in forms
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

### Exception handlers in project
- Tell the developer about ServerExceptionHandler, SharedExceptionHandler, ClientExceptionHandler, MauiExceptionHandler, WindowsExceptionHandler, WebClientExceptionHandler and how they work in different platforms.

### Best Practices Summary
1. Use `KnownException`-derived exceptions when you want to show a specific message to users
2. Use `WithData()` to add debugging context to exceptions
3. Inherit from `AppComponentBase` or `AppPageBase` to get automatic exception handling
4. Use `WrapHandled()` for event handlers in Razor files
5. Don't be afraid to throw exceptions - the framework handles them gracefully

---

At the end of Stage 5, ask: **"Do you have any questions about Stage 5, or shall we proceed to Stage 6?"**

---

# Stage 6: ASP.NET Core Identity and Authentication

In this stage, you will explain the comprehensive authentication and authorization system built into the project.

## Topics to Cover:

### Overview of Identity Features
- **Complete Identity Solution**: The project includes a production-ready ASP.NET Core Identity implementation that works seamlessly across **all platforms** (Web, MAUI, Windows) with the best possible user experience
- **Built-in Features**:
  - Sign-in and Sign-up flows
  - Two-Factor Authentication (2FA) with authenticator apps
  - Biometric-based authentication (fingerprint, face recognition on supported devices)
  - Email confirmation
  - Password reset
  - Profile management
  - Session management
  - User and Role management
  - Permission-based authorization

### Authentication Architecture

#### JWT Token-Based Authentication
- **JWT Tokens**: The project uses JWT (JSON Web Tokens) for authentication
  - **Access Token**: Short-lived token for API requests (default expiration configured in `IdentitySettings`)
  - **Refresh Token**: Long-lived token to obtain new access tokens without re-login
  - Show where tokens are managed in the codebase

#### Session Management
- **Server-Side Session Storage**: User sessions are persisted in the database
  - Sessions are tracked in the `UserSessions` table
  - Allows administrators to view and revoke active sessions
  - Show examples from the session management UI in the project

### Single Sign-On (SSO) Support
- **External Identity Providers**: The project supports integration with external authentication providers
- **Duende Identity Server Demo**: By default, the project is configured to connect to a demo Duende Identity Server 8 instance
  - When you run the project and click the **bit logo** at the beginning, you can log in using the SSO demo
  - This demonstrates how to integrate external identity providers
- **Supported Providers**: You can easily configure SSO with:
  - **Keycloak** (open-source alternative to Duende)
  - **Google**
  - **Apple**
  - **X (Twitter)**
  - **GitHub**
  - **Azure Entra ID (formerly Azure AD)**
  - And many other OAuth/OpenID Connect providers
- **Configuration**: Show where external provider settings are configured in the project

### Authorization and Access Control

#### Role-Based and Permission-Based Authorization
- **Users and Roles**: The project includes complete user and role management
  - Administrators can create roles and assign users to them
  - Show the role management UI in the project
  
- **Permissions**: Fine-grained permission system
  - Each role can have specific permissions
  - Permissions control access to different features of the application
  - Show examples of permission checks in controllers

#### Policy-Based Authorization
- **Custom Policies**: You can define authorization policies in `Shared` project's `AddAuthorizationCore` method
  - **Example**: `TFA_ENABLED` policy that requires users to have two-factor authentication enabled
  - Show the actual policy definition in the code
  - **Usage**: Policies can be checked both on the **server** (in controllers with `[Authorize(Policy = "PolicyName")]`) and on the **client** (in Blazor components with `AuthorizeView` or programmatically)
  
- **Policy Examples from Project**:
  - Show how to define a policy
  - Show how to apply it to a controller or action
  - Show how to check it in a Blazor component
  - Demonstrate both server-side and client-side policy enforcement

### Identity Configuration

#### IdentitySettings in appsettings.json
- **Location**: `src/Server/Boilerplate.Server.Api/appsettings.json`
- **Configuration Options**: Explain key identity settings that can be customized:
  - Token expiration times (access token, refresh token)
  - Password requirements (length, complexity)
  - Account lockout settings
  - Two-factor authentication settings
  - Email confirmation requirements
  - Show the actual `IdentitySettings` section in the file

### Video Tutorial
- **Highly Recommended**: To fully understand the identity features and see them in action, developers should watch this **15-minute video tutorial**:
  - **URL**: https://www.youtube.com/watch?v=79ssLqSInxc&t=53s
  - The video demonstrates:
    - Sign-in/Sign-up flows on different platforms
    - Two-factor authentication setup
    - Biometric authentication in action
    - Session management
    - User and role administration
    - And much more

### Hands-On Exploration
- **Run the Project**: Encourage the developer to run the project and explore:
  - Sign up for a new account
  - Enable 2FA
  - Try biometric login (if on a supported device)
  - Log in with the demo SSO (click the bit logo)
  - Check the admin panel for user and role management
  - View active sessions

### Security Best Practices
- **Built-in Security**: The identity system follows security best practices:
  - Passwords are hashed using ASP.NET Core Identity's secure hashing
  - JWT tokens are signed and validated
  - Refresh token rotation prevents token theft
  - Session tracking enables quick revocation
  - CSRF protection is built-in
  - Rate limiting on authentication endpoints

### Code Examples
- Show actual controller methods from `IdentityController.cs`
- Demonstrate how authentication is applied to pages and components
- Show examples of `[Authorize]` attribute usage with roles and policies

### One-Time Tokens
- Checkout references of `EmailTokenRequestedOn` and tell the developer how the app creates one-time tokens that have expiration, and only the last requested token is valid.

---

At the end of Stage 6, ask: **"Do you have any questions about Stage 6, or would you like to explore any specific topic in more depth?"**

---

# Stage 7: Blazor Pages, Components, Styling & Navigation

In this stage, you will explain the Blazor UI architecture, component structure, styling system, and navigation in the project.

## Topics to Cover:

### Component Structure (Razor, Code-Behind, SCSS)
1. **Find an example page** from the project (e.g., a page from `src/Client/Boilerplate.Client.Core/Components/Pages/`)
2. **Explain the three-file structure**:
   - **`.razor` file**: Contains the HTML/Razor markup and component structure
   - **`.razor.cs` file**: Contains the C# code-behind with component logic, event handlers, and lifecycle methods
   - **`.razor.scss` file**: Contains isolated, component-specific styles
3. **Show actual examples** from the chosen page:
   - Point out how the `.razor` file defines the UI structure
   - Show how the `.razor.cs` inherits from `AppComponentBase` or `AppPageBase`
   - Demonstrate the `.razor.scss` scoped styles
   - How the page has been added to `NavBar.razor` and `MainLayout.items.razor.cs` for navigation.
   - How `AboutPage.razor` has been added to `Boilerplate.Client.Maui`, `Boilerplate.Client.Windows` and `Boilerplate.Client.Web` projects for to have access to native platform features without using interface and dependency injection.

### SCSS Styling Architecture

#### Isolated Component Styles
- **Scoped SCSS**: Explain that `.razor.scss` files create **isolated styles** that only apply to that specific component
  - These styles are automatically scoped by Blazor and won't leak to other components
  - Similar to CSS Modules in React or scoped styles in Vue
  - Show examples from actual component `.razor.scss` files

#### Global Styles
- **App.scss**: The main global stylesheet located in `src/Client/Boilerplate.Client.Core/Styles/App.scss`
  - Contains global styles, resets, and shared CSS
  - Imports other global SCSS files
  - Show the structure of `App.scss` and what it includes

#### Theme Color Variables
- **_bit-css-variables.scss**: Tell the developer about it and show examples from the project where these variables are used in SCSS files

#### The ::deep Selector
- **Purpose**: The `::deep` selector allows you to style **child components** from a parent component's scoped stylesheet
  - Similar to React's `:global` or Vue's `:deep`
  - Find and show a **real example** from the project where `::deep` is used to style a Bit.BlazorUI component
  - Mention that each bit BlazorUI component has its own css variables for styling in addition to the `Styles` and `Classes` parameters which allows styling nested child elements directly without needing `::deep` in most cases.
  Find and show one real example of using `Styles` and `Classes` parameters from the project applied on any <Bit*> component.

### Bit.BlazorUI Documentation & DeepWiki
- **Comprehensive Documentation**: Explain that `Bit.BlazorUI` has extensive documentation at **blazorui.bitplatform.dev**
  - Every component has detailed docs with:
    - Live demos and examples
    - API reference (all parameters and properties)
    - Usage guidelines
    - Code samples
- **Automatic DeepWiki Integration**: When the developer asks questions in GitHub Copilot Chat or gives commands related to UI components:
  - The `DeepWiki_ask_question` tool is available to query the `bitfoundation/bitplatform` repository
  - This provides access to the full Bit.BlazorUI documentation
  - Developers don't need to manually search the docs - just ask naturally
- **Example Questions**:
  - "How can I implement a Grid System and layout using BitGrid and BitStack components, especially if I'm familiar with the Bootstrap grid system?"

### Navigation with PageUrls
- **PageUrls Class**: Located in `src/Shared/PageUrls.cs` and related partial files
  - Contains **strongly-typed constants** for all page routes in the application
  // In a Razor file
  <BitLink Href="@PageUrls.Dashboard">Go to Dashboard</BitLink>
  ```

### Component Base Classes
- **AppComponentBase**: Base class for components (inherited by most `.razor.cs` files)
  - Provides access to common services (NavigationManager, IStringLocalizer, etc.)
  - Enhanced lifecycle methods (`OnInitAsync`, `OnParamsSetAsync`, etc.)
  - Automatic exception handling
- **AppPageBase**: Base class for pages (extends AppComponentBase with page-specific features)
  - Additional page lifecycle hooks
  - Page-level metadata and configuration
- **Show examples** from actual component/page code-behind files

### Best Practices Summary
1. **Separation of Concerns**: Keep controls and layout in `.razor` using `BitGrid` and `BitStack`, logic in `.razor.cs`, style details in `.razor.scss`
2. **Use Theme Variables**: Always use `--bit-clr-*` variables for colors to support theming
4. **Strongly-Typed Navigation**: Use `PageUrls` constants instead of hardcoded route strings
5. **Inherit from Base Classes**: Use `AppComponentBase` or `AppPageBase` for components and pages
6. **Leverage DeepWiki**: Ask questions about Bit.BlazorUI components naturally in Copilot Chat

---

At the end of Stage 7, ask: **"Do you have any questions about Stage 7, or shall we proceed to Stage 8?"**

---

## Stage 8: Dependency Injection & Service Registration

### Instructions
1. Search for `*ServiceCollectionExtensions.cs`, `*.Services.cs` and `WebApplicationBuilderExtensions` files
2. Explain the DI architecture with these key points.
3. Find and explain the `AddSessioned` method in `IClientCoreServiceCollectionExtensions.cs`:
Tell the developer about it and how it works.

4. Create a simple availability matrix showing where each registration location works

5. Explain key rules:
   - Services in Shared work everywhere but can't access platform APIs
   - Platform-specific registrations give access to native features
   - Use appropriate lifetimes (Singleton, Scoped, Transient, Sessioned)

---

At the end of Stage 8, ask: **"Do you have any questions about Stage 8 or the dependency injection system? Would you like to see examples of adding a new service?"**

---

## Stage 9: Configuration (appsettings.json)

### Instructions
1. Explain that each project has its own `appsettings.json` and `appsettings.{environment}.json` files
2. Understand the configuration priority/hierarchy from `IConfigurationBuilderExtensions.cs` in `Client.Core`.
3. Create a simple matrix showing configuration priority:
```
Priority (Low → High):
```
For example, explain: If you add a setting in `src/Shared/appsettings.json`, it applies to all platforms unless explicitly overridden in platform-specific appsettings.json files
4. Tell how `*__Comment` works in appsettings.json files, because json doesn't support comments natively.

---

At the end of Stage 9, ask: **"Do you have any questions about the configuration system?"**

---

## Stage 10: TypeScript, Build Process & JavaScript Interop

### Instructions
1. Show `tsconfig.json` and `package.json` from `src/Client/Boilerplate.Client.Core/`
2. Explain MSBuild targets in `Boilerplate.Client.Core.csproj`: `BeforeBuildTasks` → `InstallNodejsDependencies` → `BuildJavaScript`
3. Show `Scripts/App.ts` and `Extensions/IJSRuntimeExtensions.cs` - explain how C# calls JS via `jsRuntime.InvokeAsync<T>("App.methodName")` focusing on `getTimeZone` method.
4. **Demo**: Show instructions on how to add uuid package - how to modify `package.json` using corresponding `npm install` command, import it in `App.ts`, add method, call from C# extension, build and demonstrate usage in component

---

At the end of Stage 10, ask: **"Do you have any questions about TypeScript, the build process, or JavaScript interop? Would you like to see another example of adding a different package?"**

---

# Stage 11: Blazor Modes, PreRendering & PWA

In this stage, you will explain Blazor rendering modes, pre-rendering, and PWA features.

## Topics to Cover:

### App.razor and index.html Files
1. **Find and show these files**:
   - `Server.Web/Components/App.razor`: For Blazor Server, Auto, and WebAssembly with pre-rendering
   - `Client.Web/wwwroot/index.html`: For standalone Blazor WebAssembly
   - `Client.Maui/wwwroot/index.html`: For Blazor Hybrid (also used by Client.Windows)

2. **Important**: Changes to `App.razor` usually need similar changes in both `index.html` files

### Blazor Mode & PreRendering Configuration
- **Location**: `Server.Api/appsettings.json` under `WebAppRender` section
- **Settings**:
  - `BlazorMode`: "BlazorServer" | "BlazorWebAssembly" | "BlazorAuto"
  Basicly we'd recommend using `BlazorServer` for development and `BlazorWebAssembly` for production deployment.
  More information about these can be found at https://www.reddit.com/r/Blazor/comments/1kq5eyu/this_is_not_yet_just_another_incorrect_comparison/
  - `PrerenderEnabled`: `true` for faster perceived load and SEO, `false` for loading screen
- **If you enable PreRendering**, update `Client.Web/wwwroot/service-worker.published.js` accordingly

### PWA & Service Workers
- **All modes are PWAs**: Server, WebAssembly, Auto, and Hybrid all support offline mode, installation, and push notifications
- **Service worker files**:
  - `service-worker.js`: Development
  - `service-worker.published.js`: Production/Staging
- Show `service-worker.published.js` and explain it to the developer

### IPrerenderStateService
- **When needed**: Only if you use direct `HttpClient` calls (not `IAppController`)
- **IAppController interfaces**: Pre-render state is handled automatically
- **Purpose**: Prevents duplicate API calls during pre-rendering by persisting server-fetched data
- **Example**:
```csharp
var products = await PrerenderStateService.GetValue(() => HttpClient.GetFromJsonAsync<ProductDto[]>("api/products/"));
```

---

At the end of Stage 11, ask: **"Do you have any questions about Blazor Modes, Pre-Rendering, or PWA features?"**

---

## Stage 12: Force Update System

### Instructions
1. Search for `ForceUpdateMiddleware` and `IAppUpdateService` in the codebase
2. Explain how client sends `X-App-Version` header with HttpClient/SignalR requests
3. Show how server validates version via middleware against `SupportedAppVersions` in `Server.Api/appsettings.json`
4. Explain platform-specific update behavior: Web/Windows auto-updates, Android/iOS/macOS opens store
5. Key difference: Version checked on **every request**, not just at startup - forces even active users to update

---

At the end of Stage 12, ask: **"Do you have any questions about the Force Update system?"**

---

## Stage 13: Response Caching System

In this stage, you will explain the comprehensive response caching system built into the project.

### Instructions

1. **Find and show the key caching components**:
   - `src/Shared/Attributes/AppResponseCacheAttribute.cs`
   - `src/Server/Boilerplate.Server.Shared/Services/AppResponseCachePolicy.cs`
   - `src/Server/Boilerplate.Server.Api/Services/ResponseCacheService.cs`
   - `src/Client/Boilerplate.Client.Core/Services/HttpMessageHandlers/CacheDelegatingHandler.cs`

2. **Explain the AppResponseCache attribute and AppResponseCachePolicy**:
   - The `AppResponseCache` attribute can be applied to:
     - Minimal API endpoints
     - Razor pages
     - Web API controllers
   - It caches HTML, JSON, XML, and other response types on:
     - **CDN Edge servers** (e.g., Cloudflare CDN)
     - **ASP.NET Core Output Cache** (can be configured to use Memory or Redis)
   - Show actual examples from the project where `AppResponseCache` is used (e.g., `AboutPage.razor`, `SiteMapsEndpoint.cs`, minimal API examples)

3. **Explain ResponseCacheService for cache purging**:
   - **Purpose**: Used to purge/invalidate cached responses when data changes
   - **Real-world example**:
     - A product page like https://sales.bitplatform.dev/product/10036 is cached on Cloudflare
     - When the product is updated in the admin panel at https://adminpanel.bitplatform.dev/add-edit-product/e7f8a9b0-c1d2-e3f4-5678-9012a3b4c5d6
     - The server sends a request to Cloudflare to purge/remove that page from the Edge Cache on Cloudflare servers
   - Show actual usage where `responseCacheService.PurgeProductCache` is called after update/delete operations

4. **Explain the key benefit**:
   - Every page refresh on the Sales products pages adds **zero overhead** to the server
   - The complete response is served directly from Cloudflare's edge servers (CDN)
   - **Important note**: This only applies to responses where `UserAgnostic` is not false
   - Responses for authenticated/logged-in users are **not cached** on CDN or output cache (for security/privacy reasons)

5. **Explain the multi-layer caching architecture**:
   - In addition to CDN and output cache, there are also:
     - **Browser HTTP cache**: Standard browser caching based on HTTP headers
     - **Client-side memory cache**: Managed in `CacheDelegatingHandler`
   - All of these caching layers are based on and controlled by the `AppResponseCache` attribute
   - Show how `CacheDelegatingHandler` implements client-side memory caching

---

At the end of Stage 13, ask: **"Do you have any questions about the Response Caching system?"**

---

## Stage 14: Logging and OpenTelemetry

### Instructions

1. **Explain ILogger for errors, warnings, and general information**:

2. **Explain Activity and AppActivitySource for tracking operations (count/duration)**:
   - Show `src/Shared/Services/AppActivitySource.cs` file
   - Find and demonstrate example using `AppActivitySource`

3. **Logging configuration in `src/Shared/appsettings.json`**:
   - Show the `Logging` section with different providers

4. **In-app Diagnostic Logger - extremely useful in Production for client-side troubleshooting**:
   - Opens with **Ctrl + Shift + X** or clicking 7 times on header spacer
   - Located in `AppDiagnosticModal.razor.cs`
   - Support staff can view user session logs in real-time via SignalR (`UPLOAD_DIAGNOSTIC_LOGGER_STORE` method)

5. **Easy integration with Sentry and Azure App Insights**:
   - Just set `Dsn` in `appsettings.json` for Sentry
   - Just set `ConnectionString` for Application Insights
   - All logs automatically flow to these services on all platforms.

6. **Aspire Dashboard displays all logs and metrics**:

7. **⚠️ CRITICAL WARNING**:
   - If you're adding Serilog, using App Insights direct methods, or anything other than `ILogger` and `AppActivitySource`, you probably don't understand OpenTelemetry or Microsoft.Extensions.Logging
   - Everything is already optimally configured
   - OpenTelemetry is vendor-agnostic - switch from Sentry to App Insights without code changes

---

At the end of Stage 14, ask: **"Do you have any questions about the Logging and OpenTelemetry system?"**

---

## Stage 15: CI/CD Pipeline and Environments

### Instructions
1. **Search for workflow files**: Find and review `*.yml` files.

2. **Explain environments**: Read `Directory.Build.props` and `AppEnvironment.cs` to understand how environments (Development, Staging, Production) are configured and used throughout the project.

3. **Explain the CI/CD setup**: Based on the workflow files, explain the build, test, and deployment pipelines to the developer.

4. **Important Note**: Currently, the CI/CD workflows are configured for client platforms (Android, iOS, Windows, macOS) but are not yet Aspire-friendly for backend deployment. Backend CI/CD may need additional configuration.

---

At the end of Stage 15, ask: **"Do you have any questions about CI/CD or environment configuration?"**

---

## Stage 16: Automated Testing (Unitigration Tests)

### Instructions
1. **Explain Unitigration Test concept**: Tests written as Integration Tests with full real server behavior (both UI tests and HTTP client based tests), but with the flexibility to fake specific parts of the server when needed - similar to Unit Tests - making test writing much simpler.

2. **Read test files**: Read all files in the `src/Tests` project.

3. **Explain to developer**: Based on your understanding of the test files, explain the testing architecture and approach to the developer.

---

At the end of Stage 16, ask: **"Do you have any questions about the testing approach or writing tests?"**

---

## Stage 17: Other Available Prompt Templates

### Instructions
1. **Search for prompt files**: Look for all `.prompt.md` files in `.github/prompts/` directory (excluding `getting-started.prompt.md`)
2. **Read and analyze each prompt**: Read the content of each found prompt file
3. **Explain each prompt**: For each prompt file found, provide:
   - The prompt file name
   - A clear description of what the prompt does
   - When and why a developer should use it
   - Key features or capabilities it provides

### Present the information in a clear format:
... (continue for each prompt found)

---

At the end of Stage 17, ask: **"Do you have any questions about these specialized prompts, or would you like to see examples of using any of them?\"**

---

## Stage 18: Project miscellaneous files

### Instructions
1. **Search for configuration files**: Look for the following files in the workspace root:
   - `Clean.bat` and `Clean.sh`
   - `global.json`
   - `vs-spell.dic`
   - `settings.VisualStudio.json`
   - `mcp.json`
   - `Directory.Build.props`
   - `.vsconfig`

2. **Read and explain each file**: For each file found, provide:
   - The file name and location
   - Its purpose and what it configures
   - When and why a developer should care about it
   - Key configuration options or usage instructions

---

At the end of Stage 18, ask: **"Do you have any questions about these configuration files and development tools, or would you like to explore any of them in more detail?"**

---

## Stage 19: WebAuthn and Passwordless Authentication (Advanced)

### Instructions
1. **Explain WebAuthn Overview**: Sign-in with fingerprint, Face ID, and PIN that is more secure than native biometric authentication. The bit implementation works across all platforms, although Face ID is not yet supported on Android.

2. **Search and explain the architecture**: Search for webAuthn, web-interop-app.html, ILocalHttpServer, and IExternalNavigationService. Based on your understanding of these components, explain how this feature works.

3. **Show the flow**: Demonstrate how in Blazor Hybrid, due to WebView limitations with IP-based origins, a Local HTTP Server and In-App Browser are used to make WebAuthn work.

---

At the end of Stage 19, ask: **"Do you have any questions about WebAuthn implementation?"**

---

## Final Message

After completing all stages, congratulate the developer and encourage them to explore the codebase further:

"Great job! You've completed the getting-started guide. Now you understand the core architecture of the project. Feel free to explore the codebase and ask questions about any specific component.

You can also ask your questions at https://bitplatform.dev/templates/wiki#ai-powered-wiki for additional support and community discussions.

Happy coding! 🚀"
