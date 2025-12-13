# GitHub Copilot Instructions

## Key Technologies

- **C# 14.0**
- **ASP.NET Core 10.0**
- **Blazor**: Component-based web UI framework
- **Bit.BlazorUI**: Primary UI component library

## Coding Conventions & Best Practices

1.  **Follow the established project structure**: Adhere to the defined layout for consistency.
2.  **Use Bit.BlazorUI Components**: Prioritize using components from the Bit.BlazorUI library over generic HTML to ensure UI consistency and leverage built-in features.
3.  **Embrace Nullable Reference Types**: All new code must be nullable-aware, as nullability is enabled project-wide.
4.  **Leverage Dependency Injection**: Register and resolve services using the built-in DI container.
5.  **Implement Structured Logging**: Use structured logging for clear, queryable application logs.
6.  **Use Async Programming**: Employ `async/await` for I/O-bound operations to prevent blocking threads.
7.  **Modern C#**: Write modern, concise, and efficient code by using the latest C# language features, including implicit usings and global using statements.
8. **Prefer razor.cs code-behind files**: Use `.razor.cs` files for component logic instead of @code blocks in `.razor` files.
9. **Prefer razor.css files**: Use `.razor.css` files for component styles instead of inline styles in `.razor` files.
10. **After applying changes, make sure project builds successfully**: Always verify that the project builds without errors after making changes.

## Rules

**RULE 1:** If a task (question, code modification or review) involves the use of bit BlazorUI components (e.g., `BitButton`, `BitTooltip`, `BitTextField`),
or refers to `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql`, or `bit Boilerplate`, or involves UI components without explicitly specifying their UI toolkit,
you **MUST** use the deepwiki's `ask_question` mcp tool to find the correct implementation and usage patterns of `bitfoundation/bitplatform` deep wiki before writing or changing any code.

**End of RULE 1**

**RULE 2:** If a task (question, code modification, or review) involves Microsoft technologies such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, Blazor or the `dotnet` runtime,
you **MUST** use the `microsoft.docs.mcp` server to search Microsoft's latest official documentation for detailed and up-to-date information before responding to specific or narrowly defined questions.

**End of RULE 2**

**RULE 3:** You **MUST** use the read-website-fast's `fetch` mcp tools, to gather information from URLs provided by the user.

**End of RULE 3**
