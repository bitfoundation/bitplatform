# copilot-instructions.md

### Technologies
- **C# 13.0**
- **ASP.NET Core 9.0**
- **Blazor**: Component-based web UI framework
- **.NET MAUI**: Cross-platform app development
- **ASP.NET Core Identity**: Authentication and authorization
- **Entity Framework Core**: Data access
- **SignalR**: Real-time communication
- **Hangfire**: Background job processing
- **OData**: Advanced querying capabilities
- **Bit.BlazorUI**: Primary UI component library
- **Microsoft.Extensions.AI**: AI integration
- **TypeScript**: Type-safe JavaScript development
- **SCSS**: Advanced CSS preprocessing

### Best Practices

1.  **Follow the established project structure**: Adhere to the defined layout for consistency.
2.  **Use Bit.BlazorUI Components**: Prioritize using components from the Bit.BlazorUI library over generic HTML to ensure UI consistency and leverage built-in features.
3.  **Embrace Nullable Reference Types**: All new code must be nullable-aware, as nullability is enabled project-wide.
4.  **Leverage Dependency Injection**: Register and resolve services using the built-in DI container.
5.  **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
6.  **Adhere to Security Best Practices**: Implement robust authentication and authorization patterns.
7.  **Use Async Programming**: Employ `async/await` for I/O-bound operations to prevent blocking threads.
8.  **Utilize Latest C# Features**: Write modern, concise, and efficient code by using the latest C# language features.
9.  **Leverage Implicit Usings**: Take advantage of global using statements.
10. **Respect .editorconfig**: Adhere to the `.editorconfig` file for consistent code style across all IDEs.
12. **Prefer razor.cs code-behind files**: Use `.razor.cs` files for component logic instead of @code blocks in `.razor` files.
13. **Prefer razor.scss files**: Use `.razor.scss` files for component styles instead of inline styles in `.razor` files.

### Rules for Using DeepWiki

The `deepwiki` MCP tool is essential for this project. Use it not only to answer questions but also to guide **code modifications**.
For the topics below, you **MUST** consult `deepwiki` to find correct implementations and usage patterns before writing or changing any code.

**RULE 1:** If a task (which includes answering a question or performing a code modification) involves the use of bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`), or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, or `bit Boilerplate`,
you **MUST** use the `deepwiki` mcp tool to find the correct implementation and usage patterns before writing or changing any code.

**RULE 1 Command:**
`deepwiki fetch bitfoundation/bitplatform`

**End of RULE 1**

**RULE 2:** If a task (question or code modification) involves mappers, or refers to `mapperly`, `map`, `project`, or `patch`, you **MUST** use the `deepwiki` mcp tool to find the correct implementation before writing or changing any code.

**RULE 2 Command:**
`deepwiki fetch riok/mapperly`

**End of RULE 2**

**RULE 3:** If a task (question or code modification) involves .NET MAUI, or refers to `xaml`, `Android`, `iOS`, or `Windows`, you **MUST** use the `deepwiki` mcp tool to find the correct implementation before writing or changing any code.

**RULE 3 Command:**
`deepwiki fetch dotnet/maui`

**End of RULE 3**

**RULE 4:** If a task (question or code modification) involves Hangfire, or refers to `background job` or `recurring jobs`, you **MUST** use the `deepwiki` mcp tool to find the correct implementation before writing or changing any code.

**RULE 4 Command:**
`deepwiki fetch HangfireIO/Hangfire`

**End of RULE 4**

**RULE 5:** If a task (question or code modification) involves file storage, or refers to `fluent storage` or `S3`, you **MUST** use the `deepwiki` mcp tool to find the correct implementation before writing or changing any code.

**RULE 5 Command:**
`deepwiki fetch robinrodricks/FluentStorage`

**End of RULE 5**