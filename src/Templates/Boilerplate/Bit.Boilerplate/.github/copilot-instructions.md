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

### Rules for Using MCP

**RULE 1:** If a task (question or code modification) involves the use of bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`),
or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, or `bit Boilerplate`, or involves UI components without explicitly specifying their UI toolkit,
you **MUST** use the deepwiki's `ask_question` mcp tool to find the correct implementation and usage patterns of `bitfoundation/bitplatform` deep wiki before writing or changing any code.

**End of RULE 1**

**RULE 2 Command:**
Use Playwright MCP tools like browser_navigate for URLs, browser_click for interactions, and browser_snapshot for page analysis.

**End of RULE 2**