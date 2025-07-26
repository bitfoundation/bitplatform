# GitHub Copilot Instructions

## Key Technologies
- **C# 13.0**
- **ASP.NET Core 9.0**
- **Blazor**: Component-based web UI framework
- **.NET MAUI Blazor Hybrid**: Cross-platform app development
- **ASP.NET Core Identity**: Authentication and authorization
- **Entity Framework Core**: Data access
- **SignalR**: Real-time communication
- **Hangfire**: Background job processing
- **OData**: Advanced querying capabilities
- **Bit.BlazorUI**: Primary UI component library
- **Microsoft.Extensions.AI**: AI integration
- **TypeScript**: Type-safe JavaScript development
- **SCSS**: Advanced CSS preprocessing

## Project Structure
- **Boilerplate.Server.Api**: Controllers, Mappers, DbContext, Migrations, Components for email templates, action filters, models, SignalR, server's appsettings.json
- **Boilerplate.Server.Web**: App's default startup project and entry point. Contains App.razor and other files to server Blazor Server and pre-rendering.
- **Boilerplate.Server.Shared (Also knows as Aspire's ServiceDefaults)**: Common codes shared between Server.Api and Server.Web.
- **Boilerplate.Server.AppHost**: Aspire's appsettings and Program.cs
- **Boilerplate.Shared**: Dtos, Enums, custom exceptions, shared services, resx files.
- **Boilerplate.Tests**: UI and integration tests.
- **Boilerplate.Client.Core**: Components, Pages, Layout, Client services, app.ts (TypeScript), app.scss (SCSS), and appsettings.json for the client.
- **Boilerplate.Client.Web**: Blazor WebAssembly standalone project.
- **Boilerplate.Client.Maui**: .NET MAUI based Blazor Hybrid project.
- **Boilerplate.Client.Windows**: Windows forms based Blazor Hybrid project.

## MCP Tools
- **DeepWiki**: Provides access to an extensive knowledge base for the `bitfoundation/bitplatform` repository. Use the `ask_question` tool to find the correct implementation and usage patterns for anything related to Bit.BlazorUI, `bit Bswup`, `bit Butil`, `bit Besql`, or the bit Boilerplate template.
- **Website Fetcher**: Gathers information from URLs provided by the user. Prefer the built-in `fetch` tool if available; otherwise, use the `read-website-fast` tool.

## Workflow
1.  **Problem Understanding**: Carefully analyze the user's request—whether it's a question, a code modification, or a review—to fully understand the requirements and objectives.
2.  **Codebase Investigation**: Before writing or modifying code, thoroughly examine the relevant parts of the project to understand the existing structure, logic, and potential impacts of your changes.
3.  **Research & Information Gathering**:
    *   If a task involves Bit.BlazorUI components (e.g., `BitButton`, `BitTooltip`), refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, `bit Boiler plate template`, or involves UI components without a specified UI toolkit, you **MUST** use the `DeepWiki`'s `ask_question` tool to find the correct implementation and usage patterns. This is also required for resolving build errors related to these technologies.
    *   If the user provides a URL, you **MUST** use a `fetch` tool to retrieve its content.
    *   If the user provides a git commit id/hash, you **MUST** run the `git --no-pager show <commit-id>` command to retrieve the commit details.
4.  **Create a Step-by-Step Plan**: Outline the actions and code modifications you will perform. This ensures a structured approach and helps verify that all requirements are being met.
5.  **Implementation**: Execute the plan by writing or modifying the code. Throughout this process, adhere strictly to the **Coding Conventions & Best Practices**.
6.  **Verification and Iteration**:
    *   After applying changes, you **MUST** ensure the project builds successfully. If the build fails, you must fix the issues. For build errors related to Bit.BlazorUI components (e.g., `BitButton`, `BitTooltip`), or `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, `bit Boiler plate template`, refer back to the `DeepWiki` tool as specified in step 3.
    *   Continue this process until all project requirements are fully met and the solution is in a stable, buildable state.

## CLI commands
- **Build the project**: Change directory to the `Boilerplate.Server.Web` project and run:
  ```bash
  dotnet build
  ```
- **Run the project**: Change directory to the `Boilerplate.Server.Web` project and run:
  ```bash
  dotnet run
  ```
- **Run tests**: Change directory to the `Boilerplate.Tests` project and run:
  ```bash
  dotnet test
  ```
- **Add new migrations**: Change directory to the `Boilerplate.Server.Api` project and run:
  ```bash
  dotnet ef migrations add <MigrationName> --verbose
  ```
- **Generate Resx C#**: Change directory to the `Boilerplate.Shared` project and run:
  ```bash
  dotnet build -t:PrepareResources
  ```

## Coding Conventions & Best Practices
01. **Follow the established project structure**: Adhere to the defined layout for consistency.
02. **Use Bit.BlazorUI Components**: Prioritize using components from the Bit.BlazorUI library over generic HTML to ensure UI consistency and leverage built-in features.
03. **Embrace Nullable Reference Types**: All new code must be nullable-aware, as nullability is enabled project-wide.
04. **Leverage Dependency Injection**: Register and resolve services using the built-in DI container.
05. **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
06. **Adhere to Security Best Practices**: Implement robust authentication and authorization patterns.
07. **Use Async Programming**: Employ `async/await` for I/O-bound operations to prevent blocking threads.
08. **Modern C#**: Write modern, concise, and efficient code by using the latest C# language features, including implicit usings and global using statements.
09. **Respect .editorconfig**: Adhere to the `.editorconfig` file for consistent code style across all IDEs.
10. **Prefer razor.cs code-behind files**: Use `.razor.cs` files for component logic instead of `@code` blocks in `.razor` files.
11. **Prefer razor.scss files**: Use `.razor.scss` files for component styles instead of inline styles in `.razor` files.
.razor.scss act as isolated css styles for each component, so you might need ::deep selector or !important to override styles of Bit.BlazorUI components, but before doing so, you **MUST** consult the `DeepWiki` tool to verify if there is a better way to achieve the desired styling.
12. **Use BitColor theme colors** in C#, Razor, and `_bit-css-variables.scss` SCSS files instead of custom colors to ensure dark/light mode support and maintain consistency throughout the application.
13. **Prefer bit BlazorUI components**: Use Bit.BlazorUI components like `BitButton`, `BitShimmer`, `BitCard`, `BitChart`, `BitCarousel`, etc., instead of HTML elements or other libraries to ensure consistency and leverage built-in features.
14. **Use Enhanced Lifecycle Methods**: For components inheriting from `AppComponentBase` and pages from `AppPageBase`, use these alternatives: `OnInitAsync` (instead of `OnInitializedAsync`), `OnParamsSetAsync` (instead of `OnParametersSetAsync`), and `OnAfterFirstRenderAsync` (instead of `OnAfterRenderAsync`). Always pass the `CurrentCancellationToken` to async methods that accept it.