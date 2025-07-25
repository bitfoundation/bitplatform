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
- **Boilerplate.Server.Web**: App.razor and other files to server Blazor Server and pre-rendering.
- **Boilerplate.Server.Shared (Also knows as Aspire's ServiceDefaults)**: Common codes shared between Server.Api and Server.Web.
- **Boilerplate.Server.AppHost**: Aspire's appsettings and Program.cs
- **Boilerplate.Shared**: Dtos, Enums, custom exceptions, shared services, resx files.
- **Boilerplate.Tests**: UI and integration tests.
- **Boilerplate.Client.Core**: Components, Pages, Layout, Client services, app.ts (TypeScript), app.scss (SCSS), and appsettings.json for the client.
- **Boilerplate.Client.Web**: Blazor WebAssembly standalone project.
- **Boilerplate.Client.Maui**: .NET MAUI based Blazor Hybrid project.
- **Boilerplate.Client.Windows**: Windows forms based Blazor Hybrid project.

## CLI commands
- **Build the solution**: Change directory to the `Boilerplate.Server.Web` project and run:
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
10. **Prefer razor.cs code-behind files**: Use `.razor.cs` files for component logic instead of @code blocks in `.razor` files.
11. **Prefer razor.scss files**: Use `.razor.scss` files for component styles instead of inline styles in `.razor` files.

## Rules

**RULE 1:** If a task (question, code modification or review) involves the use of bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`),
or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, or `bit Boiler plate template`, or involves UI components without explicitly specifying their UI toolkit,
you **MUST** use the deepwiki's `ask_question` mcp tool to find the correct implementation and usage patterns of `bitfoundation/bitplatform` deep wiki before writing or changing any code.

**End of RULE 1**

**RULE 2:** If a task (question, code modification, or review) involves Microsoft technologies such as C#, F#, ASP.NET Core, Aspire, Microsoft.Extensions, NuGet, Entity Framework, Blazor or the `dotnet` runtime,
you **MUST** use the `microsoft.docs.mcp` server to search Microsoft's latest official documentation for detailed and up-to-date information before responding to specific or narrowly defined questions.

**End of RULE 2**

**RULE 3:** You **MUST** use the read-website-fast's `fetch` mcp tools, to gather information from URLs provided by the user.

**End of RULE 3**

**RULE 4:** Always make sure project builds successfully after applying changes. If the project does not build, you **MUST** fix the issues before submitting the code.
For build errors related to bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`) or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`,
or `bit Boiler plate template` , you **MUST** use the deepwiki's `ask_question` mcp tool to find the correct implementation and usage patterns of `bitfoundation/bitplatform` deep wiki.

**End of RULE 4**

**RULE 5:** Avoid using !important in SCSS when working with bit BlazorUI components. Before using !important, you **MUST** use the deepwiki's `ask_question` mcp tool of `bitfoundation/bitplatform` deep wiki to validate if it's truly necessary,
as bit components typically provide built-in parameters and styling approaches that eliminate the need for CSS overrides.
**End of RULE 5**

**RULE 6:** For components inheriting from `AppComponentBase` and pages inheriting from `AppPageBase`, use these safer lifecycle method alternatives:
Use `OnInitAsync` instead of `OnInitializedAsync`
Use `OnParamsSetAsync` instead of `OnParametersSetAsync`
Use `OnAfterFirstRenderAsync` instead of `OnAfterRenderAsync`
Always pass `CurrentCancellationToken` to async methods that accept cancellation tokens.
**End of RULE 6**

**RULE 7:** Continue processing and do not stop unless all project requirements are met and project builds successfully.
**End of RULE 7**