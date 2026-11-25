# GitHub Copilot Instructions

## Key Technologies

- **C# 14.0**
- **ASP.NET Core**
- **Blazor**: Component-based web UI framework
- **.NET MAUI Blazor Hybrid**: Cross-platform app development
- **Entity Framework Core**: Data access
- **SignalR**: Real-time communication
- **Bit.BlazorUI**: Primary UI component library
- **Microsoft.Extensions.AI**: AI integration
- **TypeScript**: Type-safe JavaScript development
- **SCSS**: Advanced CSS preprocessing

## Project Structure
- **src/BlazorUI**: bit BlazorUI, its demo and its tests.
- **src/Butil, src/Bswup, src/Brouter**: bit Butil, bit Bswup and bit Brouter, each with its demo, samples and tests.
- **src/Utilities**: the other bit libraries (Bmotion, Bmage, Bsqlite, Bink and the rest), each with its demo, samples and tests.
- **src/Toolings**: Bit.Minifier, Bit.BlazorES2019, Bit.CodeAnalyzers and Bit.SourceGenerators.
- **src/Templates/BlazorEmpty**: the BlazorEmpty project template.
- **src/Websites**: the bit platform websites.

bit Boilerplate and bit CLI live in [bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric).

## Coding Conventions & Best Practices

1.  **Follow the established project structure**: Adhere to the defined layout for consistency.
2.  **Use Bit.BlazorUI Components**: Prioritize using components from the Bit.BlazorUI library over generic HTML to ensure UI consistency and leverage built-in features.
3.  **Embrace Nullable Reference Types**: All new code must be nullable-aware, as nullability is enabled project-wide.
4.  **Leverage Dependency Injection**: Register and resolve services using the built-in DI container.
5.  **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
6.  **Adhere to Security Best Practices**: Implement robust authentication and authorization patterns.
7.  **Use Async Programming**: Employ `async/await` for I/O-bound operations to prevent blocking threads.
8.   **Modern C#**: Write modern, concise, and efficient code by using the latest C# language features, including implicit usings and global using statements.
10. **Respect .editorconfig**: Adhere to the `.editorconfig` file for consistent code style across all IDEs.
11. **Prefer razor.cs code-behind files**: Use `.razor.cs` files for component logic instead of @code blocks in `.razor` files.
12. **Prefer razor.scss files**: Use `.razor.scss` files for component styles instead of inline styles in `.razor` files.
13. **After applying changes, make sure project builds successfully**: Always verify that the project builds without errors after making changes.

## Rules

**RULE 1:** If a task (question, code modification or review) involves the use of bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`),
or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Bmotion`, `bit Bink`, `bit BbScanner`, `bit Bdnd`, `bit BlazeDb`, `bit Blow`, `bit Borm`, `bit Bcrdt`, `bit Bonboarding`, `bit Bortable`, `bit Blex`, `bit Bmage`, `bit Bsqlite`, `bit Bheadless` or `bit Brouter`, or involves UI components without explicitly specifying their UI toolkit,
you **MUST** read that library's source code and README in this repository before writing or changing any code. A library that has an MCP server of its own at `https://<name in lowercase>.bitplatform.dev/mcp` (e.g. `https://bmotion.bitplatform.dev/mcp`) also answers through its tools - start with its `Search<Library>` tool and follow the calls it names.

**End of RULE 1**

**RULE 2:** If a task (question, code modification, or review) involves Microsoft technologies such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, Blazor or the `dotnet` runtime,
you **MUST** use the `microsoft_docs_search`, `microsoft_docs_fetch` and `microsoft_code_sample_search` tools of the `microsoft-learn` MCP server to search Microsoft's latest official documentation for detailed and up-to-date information before responding to specific or narrowly defined questions.

**End of RULE 2**

**RULE 3:** You **MUST** gather information from URLs provided by the user, using `fetch` or `get_web_pages` tools.

**End of RULE 3**
