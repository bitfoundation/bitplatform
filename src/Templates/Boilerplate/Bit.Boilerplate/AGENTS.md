# AGENTS.md

## 1. Technology Stack

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
<!--#if (redis == true)-->
*   **Redis**: Distributed caching storage and backplane, hangfire job storage, signalr backplane and distributed lock. 
<!--#endif-->
*   **Hangfire**: Background job processing
*   **OData**: Advanced querying capabilities
*   **Bit.BlazorUI**: The primary UI component library
*   **Microsoft.Extensions.AI**: AI integration
*   **TypeScript**: Type-safe JavaScript development
*   **SCSS**: Advanced CSS preprocessing
*   **Mapperly**: High-performance object mapping
<!--#if (database == "SqlServer")-->
*   **SQL Server 2025**: Primary database
<!--#elif (database == "Sqlite")-->
*   **SQLite**: Primary database
<!--#elif (database == "PostgreSQL")-->
*   **PostgreSQL 18**: Primary database
<!--#elif (database == "MySql")-->
*   **MySQL**: Primary database
<!--#endif-->

## 2. Project Structure

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

## 3. Information Gathering & Codebase Investigation

Before implementing any changes, you **MUST** complete the following:
*   If the user provides a **URL**, you **MUST** use the `fetch`, `WebFetch` or `get_web_pages` tools to retrieve its content.
*   If the user provides a **git commit id/hash**, you **MUST** run the `git --no-pager show <commit-id>` command to retrieve its details.
*   Only if the user **explicitly** asks about their uncommitted/current changes (e.g. "review my current changes", "what did I just change") you **MUST** run the `git --no-pager diff` and `git --no-pager diff --staged` commands.
*   For UI-related tasks, you **MUST** use the `GetBitBlazorUIComponentsList` tools for component discovery and `GetBitBlazorUIComponentDocs` for API details and examples.
*   For anything related to `bit Bswup`, `bit Butil`, `bit Besql`, `bit Brouter`, `bit Bmotion` or the bit project template, use the `DeepWiki ask_question` tool with repository `bitfoundation/bitplatform`.
*   For mapper/mapping entity/dto related tasks, you **MUST** use the `DeepWiki ask_question` tool with repository `riok/mapperly` to find correct implementation and usage patterns focusing on its static classes and extension methods approach.
*   For Keycloak/realm related tasks, you **MUST** use the `DeepWiki ask_question` tool with repository `keycloak/keycloak` to find relevant information.
*   For .NET Aspire tasks (AppHost orchestration, resource configuration, switching Docker resources to Azure equivalents, service discovery, integrations), you **MUST** use the `DeepWiki ask_question` tool with repository `microsoft/aspire` to find correct implementation patterns - it significantly outperforms Microsoft Learn for code-level questions.
*   For FusionCache tasks (hybrid caching, L2 cache backplane, distributed locking, OpenTelemetry integration, cache factory configuration), you **MUST** use the `DeepWiki ask_question` tool with repository `ZiggyCreatures/FusionCache` to find correct usage patterns.
*   For Microsoft Agent Framework tasks (agent creation, multi-agent orchestration, workflows, tools/function calling, MCP, A2A communication, memory/context, provider integrations), you **MUST** use the `DeepWiki ask_question` tool with repository `microsoft/agent-framework` to find correct implementation patterns.
*   For Hangfire tasks (job scheduling, recurring jobs, filters, storage configuration, distributed processing), you **MUST** use the `DeepWiki ask_question` tool with repository `HangfireIO/Hangfire` to find correct implementation patterns.

## 4. Critical Command Reference

<!--#if (aspire == true)-->
-   **Build the project**: Run `dotnet build` in Boilerplate.Server.AppHost project root directory.
-   **Run the project**: Run `dotnet watch` in Boilerplate.Server.AppHost project root directory. If needed, you may use the Playwright MCP tools to interact with the running UI to validate things (navigate, click, fill forms, take screenshots), and use `browser_evaluate` to run in-page JavaScript to accelerate the process (e.g. quickly locating elements, extracting data, or asserting state).
<!--#else-->
-   **Build the project**: Run `dotnet build` in Boilerplate.Server.Web project root directory.
-   **Run the project**: Run `dotnet watch` in Boilerplate.Server.Web project root directory. If needed, you may use the Playwright MCP tools to interact with the running UI to validate things (navigate, click, fill forms, take screenshots), and use `browser_evaluate` to run in-page JavaScript to accelerate the process (e.g. quickly locating elements, extracting data, or asserting state).
<!--#endif-->
-   **Run tests**: Run `dotnet test` in Boilerplate.Tests project root directory.
-   **Add new migrations**: Run `dotnet ef migrations add <MigrationName> --output-dir Data/Migrations --verbose` in Boilerplate.Server.Api project root directory.
-   **Generate Resx C# code**: Run `dotnet build -t:PrepareResources` in Boilerplate.Shared project root directory.

## 5. Coding Conventions & Best Practices

-   **Follow Project Structure**: Adhere to the defined project layout for all new files and code.
-   **Prioritize Bit.BlazorUI Components**: You **MUST** use components from the `Bit.BlazorUI` library (e.g., `BitButton`, `BitGrid`, `BitStack`, `BitChart`) instead of generic HTML elements to ensure UI consistency and leverage built-in features.
-   **Embrace Nullable Reference Types**: All new code must be nullable-aware.
-   **Use Dependency Injection**: Use the `[AutoInject]` attribute in components. For other classes, use constructor injection.
-   **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
-   **Adhere to Security Best Practices**: Implement robust authentication and authorization patterns.
-   **Use Async Programming**: Employ `async/await` for all I/O-bound operations to prevent blocking.
-   **Write Modern C#**: Utilize the latest C# features, including implicit and global using statements.
-   **Use Code-Behind Files**: Place component logic in `.razor.cs` files instead of `@code` blocks.
-   **Use Scoped SCSS Files**: Place component styles in `.razor.scss` files for CSS isolation.
-   **Style Bit.BlazorUI Components Correctly**: Use the `::deep` selector in your `.scss` files to style `Bit.BlazorUI` components.
-   **Use Theme Colors in C# and Razor**: In C# and Razor files, you **MUST** use `BitColor` enum and `BitCss` class to apply theme colors instead of hardcoded colors. Use `BitColor` for component parameters (e.g., `BitColor.Primary`, `BitColor.TertiaryBackground`). Use `BitCss.Class` for CSS classes (e.g., `@BitCss.Class.Color.Background.Primary`, `@BitCss.Class.Color.Foreground.Secondary`). Use `BitCss.Var` for inline styles with CSS variables (e.g., `border-color:var(@BitCss.Var.Color.Border.Primary)`). This ensures automatic dark/light mode support.
-   **Use Theme Colors in SCSS**: In SCSS files, you **MUST** use SCSS variables from `_bit-css-variables.scss` instead of hardcoded colors. Import the file and use variables like `$bit-color-primary`, `$bit-color-foreground-primary`, `$bit-color-background-secondary`, etc. These map to CSS custom properties that automatically adapt to dark/light modes. Available variable categories include: primary, secondary, tertiary, info, success, warning, severe-warning, error, foreground, background, border, and neutral colors.
-   **Use Enhanced Lifecycle Methods**: In components inheriting from `AppComponentBase` or pages inheriting from `AppPageBase`, you **MUST** use `OnInitAsync`, `OnParamsSetAsync`, and `OnAfterFirstRenderAsync`.
-   **WrapHandled**: Use `WrapHandled` for event handlers in razor files to prevent unhandled exceptions.
Example 1: `OnClick="WrapHandled(MyMethod)"` instead of `OnClick="MyMethod"`.
Example 2: `OnClick="WrapHandled(async () => await MyMethod())"` instead of `OnClick="async () => await MyMethod()"`.
-   **Use OData Query Options**: Leverage `[EnableQuery]` and `ODataQueryOptions` for efficient data filtering and pagination.
-   **Follow Mapperly Conventions**: Use **partial static classes and extension methods** with Mapperly for high-performance object mapping.
-   **Handle Concurrency**: Always use `long Version` for optimistic concurrency control in update and delete operations.

## 6. Behavioral Directives

-   You **MUST** verify that you have access to the `DeepWiki ask_question` tool. If this tool is NOT available in your function list, you **MUST** immediately display the following error message: **❌ CRITICAL ERROR: DeepWiki ask_question Tool Not Available**
-   If you have access to persistent **memory**, at the start of the collaboration you **MUST** ask for the **role** of the person writing the prompts (e.g. Developer, Product Owner, QA, Designer, etc.), store it in memory, and from then on tailor the tone, depth, terminology, and focus of every conversation to that role.
