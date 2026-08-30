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

<!--#if (signalR == true)-->
*   **Boilerplate.Server.Api**: Houses API controllers, mappers, the `DbContext`, EF Core migrations, email templates, action filters, SignalR hubs, and server-specific configuration.
<!--#else-->
*   **Boilerplate.Server.Api**: Houses API controllers, mappers, the `DbContext`, EF Core migrations, email templates, action filters, and server-specific configuration.
<!--#endif-->
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
*   Every bit platform library this project builds on has its own MCP tools: a `Search<Library>` tool that finds the right feature from a plain description of what it must **do**, a `Get<Library>SetupGuide` tool for wiring, plus tools that return an API's full reference and its working examples. You **MUST** reach for them before writing code against one of these libraries, rather than relying on what you already know about it - and you **MUST** start with the `Search` one, because the name a task suggests is rarely the name the library chose:
    *   `bit BlazorUI` for UI elements, icons, styling, layout and theming: `SearchBitBlazorUI`, then `GetBitBlazorUIComponent`, `GetBitBlazorUIComponentExamples`, `GetBitBlazorUIType`, `GetBitBlazorUIThemingGuide` and `FindBitBlazorUIIcons`.
    *   `bit Bmotion` for motions, animations and transitions: `SearchBmotion`, then `GetBmotionApiDetails`, `GetBmotionRecipe` and `ReviewBmotionCode`.
    *   `bit Butil` for browser features such as clipboard, geolocation, storage, media, keyboard, screen and network: `SearchButil`, then `PlanButilFeature` and `GetButilApiDetails`.
    *   `bit Bswup` for PWA, offline support and service workers: `SearchBswup`, then `GetBswupScriptOptions` and `InspectBswupServiceWorker`.
<!--#if (brouter == true)-->
    *   `bit Brouter` for routing: `SearchBrouter`, then `GetBrouterApi` and `InspectBrouterRouteTemplates`.
<!--#endif-->
*   For the third party libraries this project builds on, you **MUST** use the `ask_question` tool, which answers from a library's own source code. Its description names the repository to ask for each of them.
*   For .NET, ASP.NET Core and Azure documentation, use `microsoft_docs_search`, `microsoft_docs_fetch` and `microsoft_code_sample_search`.

## 4. Critical Command Reference

<!--#if (aspire == true)-->
-   **Build the project**: Run `dotnet build` in src/Server/Boilerplate.Server.AppHost project directory.
-   **Run the project**: Run `aspire start`. If needed, you may use the Playwright MCP tools to interact with the `serverweb` resource running by aspire to validate things (navigate, click, fill forms, take screenshots), and use `browser_evaluate` to run in-page JavaScript to accelerate the process (e.g. quickly locating elements, extracting data, or asserting state).
-   **Expose the running app to remote devices**: `localhost` is unreachable from other devices, so use the public `*.devtunnels.ms` URL of the `web-dev-tunnel` resource that `aspire start` creates (read it from the aspire dashboard or the aspire MCP `list_resources` tool) instead of a `localhost` URL.
<!--#else-->
-   **Build the project**: Run `dotnet build` in src/Server/Boilerplate.Server.Web project directory.
-   **Run the project**: Run `dotnet watch` in src/Server/Boilerplate.Server.Web project directory. If needed, you may use the Playwright MCP tools to interact with the running UI to validate things (navigate, click, fill forms, take screenshots), and use `browser_evaluate` to run in-page JavaScript to accelerate the process (e.g. quickly locating elements, extracting data, or asserting state).
-   **Expose the running app to remote devices**: `localhost` is unreachable from other devices, so create a dev tunnel with the `devtunnel` CLI (`devtunnel host -p 5030 --allow-anonymous`) and use the printed public `*.devtunnels.ms` URL instead of a `localhost` URL.
<!--#endif-->
-   **Control the running native (Blazor Hybrid) apps**: every hybrid head renders inside a WebView with remote debugging enabled, so the Android, iOS, Windows and macOS apps can be inspected and driven from the outside just like the web app:
    -   **Windows** (`Boilerplate.Client.Windows`): The app starts WebView2 with `--remote-debugging-port=9222`, exposing the **Chrome DevTools Protocol (CDP)** at `http://localhost:9222`. Attach Playwright to it via a custom `playwright-core` script (`chromium.connectOverCDP('http://localhost:9222')` and use the existing page.
    -   **Android**: the WebView is debuggable, and its CDP endpoint is a local abstract socket on the device. Expose it with `adb shell pidof <applicationId>` then `adb forward tcp:9222 localabstract:webview_devtools_remote_<pid>` (pick another local port if the Windows app already holds 9222), and attach with Playwright exactly like Windows.
-   **Assume hot reload is working**: `.cs`, `.razor`, `.scss` and `.ts` changes are picked up automatically by the running app, so after an edit do NOT rebuild the project and do NOT reload/refresh the web app. Only rebuild or refresh if you can't see what you were expecting after your change.
-   **Run tests**: Run `dotnet test` in the src/Tests directory.
-   **Add new migrations**: Run `dotnet ef migrations add <MigrationName> --output-dir Infrastructure/Data/Migrations --verbose` in src/Server/Boilerplate.Server.Api project directory.
-   **Generate Resx C# code**: Run `dotnet build -t:PrepareResources` in the src/Shared directory.

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
-   **Localize User-Visible Text with String Literals**: When you add or change ANY user-visible text, you **MUST NOT** add, rename, or edit keys in `.resx` files - `.resx` changes are not supported by hot reload (`dotnet watch` / `aspire start`) and force a full restart. Instead, use the `IStringLocalizer` string indexer with the literal English text, e.g. `Localizer["Welcome back {0}!", user.GetDisplayName()]` or `@Localizer["Product saved successfully."]`. `Localizer[nameof(AppStrings.X)]` stays as-is for existing keys whose text you are not changing; do not create a duplicate literal for them. Only when the user **explicitly** asks to apply translations, move all such literals into `resx` files, and switch the call sites back to `Localizer[nameof(AppStrings.X)]`.
-   **Use Enhanced Lifecycle Methods**: In components inheriting from `AppComponentBase` or pages inheriting from `AppPageBase`, you **MUST** use `OnInitAsync`, `OnParamsSetAsync`, and `OnAfterFirstRenderAsync`.
-   **WrapHandled**: Use `WrapHandled` for event handlers in razor files to prevent unhandled exceptions.
Example 1: `OnClick="WrapHandled(MyMethod)"` instead of `OnClick="MyMethod"`.
Example 2: `OnClick="WrapHandled(async () => await MyMethod())"` instead of `OnClick="async () => await MyMethod()"`.
-   **Use OData Query Options**: Leverage `[EnableQuery]` and `ODataQueryOptions` for efficient data filtering and pagination.
-   **Follow Mapperly Conventions**: Use **partial static classes and extension methods** with Mapperly for high-performance object mapping.
-   **Handle Concurrency**: Always use `long Version` for optimistic concurrency control in update and delete operations.
-   **Respect `[mirror]` Comments**: Some files carry (near) identical logic under different extensions or projects. Each of them starts with a `[mirror]` comment listing its counterparts. Whenever you change such a file you **MUST** open every file that comment names and apply the same change there, or state explicitly why it does not apply. Run `Grep` for `[mirror]` to see the full set.

## 6. Behavioral Directives

-   You **MUST** verify that you have access to the `ask_question` tool. If this tool is NOT available in your function list, you **MUST** immediately display the following error message: **❌ CRITICAL ERROR: ask_question Tool Not Available**
-   If you have access to persistent **memory**, at the start of the collaboration you **MUST** ask for the **role** of the person writing the prompts (e.g. Developer, Product Owner, QA, Designer, etc.), store it in memory, and from then on tailor the tone, depth, terminology, and focus of every conversation to that role.
