# GitHub Copilot Instructions

## 1. Core Principles

As an expert AI assistant for this project, your actions must be guided by these core principles:

*   **Proactive Research:** Never assume. Always begin UI-related tasks by consulting the knowledge base. Your primary directive is to use the provided tools to understand the `bitplatform` ecosystem *before* writing any code.
*   **Structured Planning:** Do not implement changes impulsively. You must first analyze the request, investigate the codebase, and formulate a detailed, step-by-step plan. This plan is your blueprint for success.
*   **Rigorous Verification:** After every implementation phase, you must verify your work. This includes, at a minimum, ensuring the project builds successfully. You are responsible for identifying and fixing errors your changes introduce.
*   **Strict Adherence to Conventions:** The project's quality and maintainability depend on consistency. You must strictly follow all established coding conventions and best practices outlined in this document.

## 2. Technology Stack

You will be working with the following key technologies:

*   **C# 14.0**
*   **ASP.NET Core 10.0**
*   **Blazor**: Component-based web UI framework
*   **.NET MAUI Blazor Hybrid**: Cross-platform app development
*   **ASP.NET Core Identity**: Authentication and authorization
*   **Entity Framework Core**: Data access
<!--#if (signalR == true)-->
*   **SignalR**: Real-time communication
<!--#endif-->
*   **Hangfire**: Background job processing
*   **OData**: Advanced querying capabilities
*   **Bit.BlazorUI**: The primary UI component library
*   **Microsoft.Extensions.AI**: AI integration
*   **TypeScript**: Type-safe JavaScript development
*   **SCSS**: Advanced CSS preprocessing
*   **Mapperly**: High-performance object mapping
<!--#if (database == "SqlServer")-->
*   **SQL Server**: Primary database
<!--#elif (database == "Sqlite")-->
*   **SQLite**: Primary database
<!--#elif (database == "PostgreSQL")-->
*   **PostgreSQL**: Primary database
<!--#elif (database == "MySql")-->
    **MySQL**: Primary database
<!--#endif-->

## 3. Project Structure

The solution is organized into the following projects. Understand their roles to locate and modify the correct files.

*   **Boilerplate.Server.Api**: Houses API controllers, mappers, the `DbContext`, EF Core migrations, email templates, action filters, SignalR hubs, and server-specific configuration.
*   **Boilerplate.Server.Web**: The application's default startup project and entry point. It hosts `App.razor` and configures Blazor Server and server-side rendering (SSR).
*   **Boilerplate.Server.Shared**: (Also known as Aspire's ServiceDefaults) Contains common code shared between the `Boilerplate.Server.Api` and `Boilerplate.Server.Web` projects.
<!--#if (aspire == true)-->
*   **Boilerplate.Server.AppHost**: Manages the .NET Aspire configuration and orchestration.
<!--#endif-->
*   **Boilerplate.Shared**: Contains shared DTOs, enums, custom exceptions, shared services, and `.resx` resource files.
*   **Boilerplate.Tests**: Contains all UI and integration tests.
*   **Boilerplate.Client.Core**: The heart of the client application. Contains all shared Blazor components, pages, layouts, client-side services, and the primary `App.ts` and `App.scss` files.
*   **Boilerplate.Client.Web**: The Blazor WebAssembly (WASM) standalone project.
*   **Boilerplate.Client.Maui**: The .NET MAUI Blazor Hybrid project for native mobile and desktop apps.
*   **Boilerplate.Client.Windows**: The Windows Forms Blazor Hybrid project.

## 4. Available Tooling

-   **DeepWiki**: Provides access to an extensive knowledge base for the `bitfoundation/bitplatform` and `riok/mapperly` repositories.
-   **Website Fetcher**: Gathers information from URLs provided by the user, using `fetch` or `get_web_pages` tools.

## 5. Mandatory Workflow

You **MUST** follow this workflow for every request. Do not deviate.

### Step 1: Deconstruct the Request
Carefully analyze the user's prompt. Identify the core objectives, whether it is a question, a code modification, or a review.

### Step 2: Information Gathering & Codebase Investigation
Before writing code, investigate thoroughly.
*   If the user provides a **URL**, you **MUST** use the `fetch` or `get_web_pages` tools to retrieve its content.
*   If the user provides a **git commit id/hash**, you **MUST** run the `git --no-pager show <commit-id>` command to retrieve its details.
*   If the user talked about current changes in the codebase, you **MUST** run the `git --no-pager diff` and `git --no-pager diff --staged` commands to see the differences.
*   For UI-related tasks, you **MUST** first ask `DeepWiki`: *"What features does BitPlatform offer to help me complete this task? [USER'S ORIGINAL REQUEST]"*
*   For anything related to `Bit.BlazorUI`, `bit Bswup`, `bit Butil`, `bit Besql`, or the bit project template, you **MUST** use the `DeepWiki_ask_question` tool with repository `bitfoundation/bitplatform` to find relevant information.
*   For mapper/mapping entity/dto related tasks, you **MUST** use the `DeepWiki_ask_question` tool with repository `riok/mapperly` to find correct implementation and usage patterns focusing on its static classes and extension methods approach.
*   **🚨 CRITICAL TOOL REQUIREMENT**: You **MUST** verify that you have access to the `DeepWiki_ask_question` tool. If this tool is NOT available in your function list, you **MUST** immediately display the following error message:
**❌ CRITICAL ERROR: DeepWiki_ask_question Tool Not Available**

### Step 3: Formulate a Detailed Plan
Create a comprehensive, step-by-step plan. This plan must outline:
*   The files you will create or modify.
*   The specific changes you will make (e.g., "Add a `BitButton` to `MyComponent.razor`").
*   A brief justification for each change, referencing your research from DeepWiki and your analysis of the codebase.

### Step 4: Execute the Plan
Implement the changes exactly as described in your plan. Adhere strictly to the **Coding Conventions & Best Practices** during this phase.

### Step 5: Verify, Test, and Refine
After applying changes, you **MUST** verify the integrity of the application.
1.  **Build the Project**: Run a build to ensure your changes have not introduced compilation errors. This is mandatory.
2.  **Fix Build Errors**: If the build fails, you must fix it. For errors related to the `bitplatform`, you **MUST** go back to Step 3 and use `DeepWiki` to find the correct implementation.
3.  **Iterate**: Continue this cycle of implementation and verification until all requirements are met and the solution is in a stable, buildable state.

## 6. Behavioral Directives

*   **Be Decisive**: Do not ask for permission to proceed or for a review of your plan. Directly state your plan and proceed with the implementation.
*   **Execute Commands Individually**: **Never** chain CLI commands with `&&`. Execute each command in a separate step.
*   **Getting started**: When a developer first interacts with you with a message like `Run getting started`, you **MUST** proactively follow `.github/prompts/getting-started.prompt.md`.

## 7. Critical Command Reference

-   **Build the project**: Run `dotnet build` in Boilerplate.Server.Web project root directory.
-   **Run the project**: Run `dotnet run` in Boilerplate.Server.Web project root directory.
-   **Run tests**: Run `dotnet test` in Boilerplate.Tests project root directory.
-   **Add new migrations**: Run `dotnet ef migrations add <MigrationName> --output-dir Data/Migrations --verbose` in Boilerplate.Server.Api project root directory.
-   **Generate Resx C# code**: Run `dotnet build -t:PrepareResources` in Boilerplate.Shared project root directory (It's automatically done during build).

## 8. Coding Conventions & Best Practices

01. **Follow Project Structure**: Adhere to the defined project layout for all new files and code.
02. **Prioritize Bit.BlazorUI Components**: You **MUST** use components from the `Bit.BlazorUI` library (e.g., `BitButton`, `BitTextField`, `BitChart`) instead of generic HTML elements to ensure UI consistency and leverage built-in features.
03. **Embrace Nullable Reference Types**: All new code must be nullable-aware.
04. **Use Dependency Injection**: Use the `[AutoInject]` attribute in components. For other classes, use constructor injection.
05. **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
06. **Adhere to Security Best Practices**: Implement robust authentication and authorization patterns.
07. **Use Async Programming**: Employ `async/await` for all I/O-bound operations to prevent blocking.
08. **Write Modern C#**: Utilize the latest C# features, including implicit and global using statements.
09. **Respect .editorconfig**: Adhere to the `.editorconfig` settings for consistent code style.
10. **Use Code-Behind Files**: Place component logic in `.razor.cs` files instead of `@code` blocks.
11. **Use Scoped SCSS Files**: Place component styles in `.razor.scss` files for CSS isolation.
12. **Style Bit.BlazorUI Components Correctly**: Use the `::deep` selector in your `.scss` files to style `Bit.BlazorUI` components.
13. **Use Theme Colors**: You **MUST** use `BitColor` theme variables in C#, Razor, and SCSS files (`_bit-css-variables.scss`) to support dark/light modes. Do not use hardcoded colors.
14. **Use Enhanced Lifecycle Methods**: In components inheriting from `AppComponentBase` or pages inheriting from `AppPageBase`, you **MUST** use `OnInitAsync`, `OnParamsSetAsync`, and `OnAfterFirstRenderAsync`.
15. **WrapHandled**: Use `WrapHandled` for event handlers in razor files to prevent unhandled exceptions.
Example 1: `OnClick="WrapHandled(MyMethod)"` instead of `OnClick="MyMethod"`.
Example 2: `OnClick="WrapHandled(async () => await MyMethod())"` instead of `OnClick="async () => await MyMethod()"`.
16. **Use OData Query Options**: Leverage `[EnableQuery]` and `ODataQueryOptions` for efficient data filtering and pagination.
17. **Follow Mapperly Conventions**: Use **partial static classes and extensions methods** with Mapperly for high-performance object mapping.
18. **Handle Concurrency**: Always use `ConcurrencyStamp` for optimistic concurrency control in update and delete operations.

## Instructions for adding new model/entity to ef-core DbContext / Database
Create the Entity Model
- **Location**: `Boilerplate.Server.Api's Models folder`
- **Requirements**:
  - Include `Id`, `ConcurrencyStamp` properties
  - Add appropriate navigation properties
  - Use nullable reference types
  - Add data annotations as needed

Create the EntityTypeConfiguration
- **Location**: `Boilerplate.Server.Api's Data/Configuration folder`
  - Implement `IEntityTypeConfiguration<{EntityName}>`
  - Configure unique indexes, relationships
  - Add `DbSet<{EntityName}>` to AppDbContext
  - Add ef-core migration

## Instructions for adding new DTO and Mapper
Create the DTO
- **Location**: `Boilerplate.Shared's Dtos folder`
- **Requirements**:
  - Use `[DtoResourceType(typeof(AppStrings))]` attribute
  - Add validation attributes: `[Required]`, `[MaxLength]`, `[Display]`
  - Use `nameof(AppStrings.PropertyName)` for error messages and display names
  - Include `Id`, `ConcurrencyStamp` properties
  - Add calculated properties if needed (e.g., `ProductsCount`)
  - Add `[JsonSerializable(typeof({DtoName}))]` to AppJsonContext.cs

Create the Mapper
- **Location**: `Boilerplate.Server.Api's Mappers folders`
- **Requirements**:
  - Use `[Mapper]` attribute from Mapperly
  - Create `static partial class {MapperName}Mapper`
  - Add projection method: `public static partial IQueryable<{DtoName}> Project(this IQueryable<{EntityName}> query);`
  - Add mapping methods: `Map()`, `Patch()` for CRUD operations
  - Use `[MapProperty]` for complex mappings if needed

#### Instructions for creating Strongly Typed Http Client Wrapper to Call Backend API
- **Location**: `Boilerplate.Shared project's Controllers folder`
- **Requirements**:
  - Inherit from `IAppController`
  - Add `[Route("api/[controller]/[action]/")]` attribute
  - Add `[AuthorizedApi]` if authentication required
  - Always Use `CancellationToken` parameters
  - The return type should be `Task<T>` or Task<T> where T is JSON Serializable type like DTO, int, or List<Dto>
  - If Backend API's action returns `IQueryable<T>`, use `Task<List<T>>` as return type with `=> default!`
  - If Backend API's action returns `IActionResult`, use `Produces<T>` attribute to specify the response type with `=> default!`
  - If Backend API accepts ODataQueryOptions, simply ignore it

#### Instructions to create Backend API Controllers
- **Location**: `Boilerplate.Server.Api's Controllers folder`
- **Requirements**:
  - Inherit from `AppControllerBase`
  - Implement the corresponding IAppController interface
  - Add appropriate authorization attributes
  - Use `[EnableQuery]` for GET endpoints with OData support
  - Implement validation in private methods
  - Use `Project()` for querying and mapping
  - Handle resource not found scenarios using ResourceNotFoundException.
