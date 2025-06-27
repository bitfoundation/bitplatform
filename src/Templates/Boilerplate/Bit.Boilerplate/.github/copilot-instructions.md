# copilot-instructions.md

### Architecture & Project Structure

- **Client Projects**: Multiple platform-specific clients sharing core infrastructure 
  - `Client.Core`: Blazor pages, shared components, service, typescript and SCSS files.
  - `Client.Web`: PWA Blazor WebAssembly.
  - `Client.Maui`: Cross-platform MAUI based Blazor Hybrid app.
  - `Client.Windows`: Windows forms based Blazor Hybrid app.

- **Server Projects**: API and web
  - `Server.Api`: Web API controllers, entity framework core db context, type configurations and migrations plus SignalR, Hangfire, models, services and mappers.
  - `Server.Web`: Blazor Server, Auto and WebAssembly, with or without pre-rendering.

- **Shared**: Dto classes, enums, interfaces, resx resources etc.

- **Boilerplate.Tests**: Integration api and UI tests using Playwright and MSTest.

### Building the Application
Navigate to the server directory and run:
```bash
cd src/Server/Boilerplate.Server.Web
dotnet build
```

### Running the Application
To run the application, execute the following command in the server directory:
```bash
cd src/Server/Boilerplate.Server.Web
dotnet run
```

### Testing the Application
To run the tests, navigate to the test project directory and execute:
```bash
cd src/Tests
dotnet test
```

### Best Practices

1. **Follow the established project structure**
2. **Use Bit.BlazorUI components** - Do not use generic HTML/CSS/JS solutions for these components.
3. **Follow nullable reference type conventions** - all new code should be nullable-aware
4. **Use dependency injection** - leverage the built-in DI container for service registration
5. **Implement proper logging** - use structured logging throughout the application
6. **Follow security best practices** - use proper authentication and authorization patterns
7. **Async Programming** - prefer async/await for I/O-bound operations, avoid blocking calls
8. **C# 13.0**: Use latest language features
9. **Nullable Reference Types**: Enabled project-wide
10. **Implicit Usings**: Leverage global using statements
11. **.editorconfig**: Use for consistent code style across IDEs

### Rules for Using DeepWiki

**RULE 1:** If a question mentions or implies the use of a bit BlazorUI components (like `BitButton`, `BitTooltip`, `BitTextField`),
or refers to the `bitplatform`, `bit Bswup`, `bit Butil`, `bit Besql` and `bit Boilerplate`, you **MUST** use the `deepwiki` mcp tool to
find the correct implementation before writing any code.

**RULE 1 Command:**
`deepwiki fetch bitfoundation/bitplatform`

**End of RULE 1**

**RULE 2:** If a question mentions or implies the use of a mappers,
or refers to the `mapperly`, `map`, `project`, `patch`, you **MUST** use the `deepwiki` mcp tool to
find the correct implementation before writing any code.

**RULE 2 Command:**
`deepwiki fetch riok/mapperly`

**End of RULE 2**