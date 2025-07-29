# GitHub Copilot Instructions

## 1. Core Principles

As an expert AI assistant for this project, your actions must be guided by these core principles:

*   **Proactive Research:** Never assume. Always begin UI-related tasks by consulting the knowledge base. Your primary directive is to use the provided tools to understand the `bitplatform` ecosystem *before* writing any code.
*   **Structured Planning:** Do not implement changes impulsively. You must first analyze the request, investigate the codebase, and formulate a detailed, step-by-step plan. This plan is your blueprint for success.
*   **Rigorous Verification:** After every implementation phase, you must verify your work. This includes, at a minimum, ensuring the project builds successfully. You are responsible for identifying and fixing errors your changes introduce.
*   **Strict Adherence to Conventions:** The project's quality and maintainability depend on consistency. You must strictly follow all established coding conventions and best practices outlined in this document.

## 2. Technology Stack

You will be working with the following key technologies:

*   **C# 13.0**
*   **ASP.NET Core 9.0**
*   **Blazor**: Component-based web UI framework
*   **.NET MAUI Blazor Hybrid**: Cross-platform app development
*   **ASP.NET Core Identity**: Authentication and authorization
*   **Entity Framework Core**: Data access
*   **SignalR**: Real-time communication
*   **Hangfire**: Background job processing
*   **OData**: Advanced querying capabilities
*   **Bit.BlazorUI**: The primary UI component library
*   **Microsoft.Extensions.AI**: AI integration
*   **TypeScript**: Type-safe JavaScript development
*   **SCSS**: Advanced CSS preprocessing

## 3. Project Structure

The solution is organized into the following projects. Understand their roles to locate and modify the correct files.

*   **Boilerplate.Server.Api**: Houses API controllers, mappers, the `DbContext`, EF Core migrations, email templates, action filters, SignalR hubs, and server-specific configuration.
*   **Boilerplate.Server.Web**: The application's default startup project and entry point. It hosts `App.razor` and configures Blazor Server and server-side rendering (SSR).
*   **Boilerplate.Server.Shared**: (Also known as Aspire's ServiceDefaults) Contains common code shared between the `Server.Api` and `Server.Web` projects.
*   **Boilerplate.Server.AppHost**: Manages the .NET Aspire configuration and orchestration.
*   **Boilerplate.Shared**: Contains shared DTOs, enums, custom exceptions, shared services, and `.resx` resource files.
*   **Boilerplate.Tests**: Contains all UI and integration tests.
*   **Boilerplate.Client.Core**: The heart of the client application. Contains all shared Blazor components, pages, layouts, client-side services, and the primary `App.ts` and `App.scss` files.
*   **Boilerplate.Client.Web**: The Blazor WebAssembly (WASM) standalone project.
*   **Boilerplate.Client.Maui**: The .NET MAUI Blazor Hybrid project for native mobile and desktop apps.
*   **Boilerplate.Client.Windows**: The Windows Forms Blazor Hybrid project.

## 4. Available Tooling

-   **DeepWiki**: Provides access to an extensive knowledge base for the `bitfoundation/bitplatform` repository. Use the `mcp_deepwiki_ask_question` tool to find the correct implementation and usage patterns for anything related to Bit.BlazorUI, `bit Bswup`, `bit Butil`, `bit Besql`, or the bit Boilerplate template.
-   **Website Fetcher**: Gathers information from URLs provided by the user. Prefer the built-in `fetch` tool if available; otherwise, use the `read-website-fast` tool.

## 5. Mandatory Workflow

You **MUST** follow this workflow for every request. Do not deviate.

### Step 1: Deconstruct the Request
Carefully analyze the user's prompt. Identify the core objectives, whether it is a question, a code modification, or a review.

### Step 2: Information Gathering & Codebase Investigation
Before writing code, investigate thoroughly.
*   If the user provides a **URL**, you **MUST** use the `fetch` tool to retrieve its content.
*   If the user provides a **git commit id/hash**, you **MUST** run the `git --no-pager show <commit-id>` command to retrieve its details.
*   For UI-related tasks, you **MUST** first ask `DeepWiki`: *"What features does BitPlatform offer to help me complete this task? [USER'S ORIGINAL REQUEST]"*

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

## 7. Critical Command Reference

-   **Build the project**: First change directory to src/Server/Boilerplate.Server.Web and run `dotnet build`
-   **Run the project**: First change directory to src/Server/Boilerplate.Server.Web and run `dotnet run`
-   **Run tests**: First change directory to src/Tests/Boilerplate.Tests and run `dotnet test`
-   **Add new migrations**: First change directory to src/Server/Boilerplate.Server.Api and run `dotnet ef migrations add <MigrationName> --verbose`
-   **Generate Resx C# code**: First change directory to src/Shared and run `dotnet build -t:PrepareResources`

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