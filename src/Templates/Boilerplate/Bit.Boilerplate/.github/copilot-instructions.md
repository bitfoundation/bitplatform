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

- **Shared**: Dto classes, enums, interfaces etc.

- **Boilerplate.Tests**: Integration api and UI tests using Playwright and MSTest.

### Technologies
- **C# 13.0**
- **ASP.NET Core 9.0**
- **Blazor**: Component-based web UI framework
- **MAUI**: Cross-platform app development
- **ASP.NET Core Identity**: Authentication and authorization
- **Entity Framework Core**: Data access
- **SignalR**: Real-time communication 
- **Hangfire**: Background job processing
- **OData**: Advanced querying capabilities
- **Bit.BlazorUI**: Primary UI framework 
- **Microsoft.Extensions.AI**: AI integration
- **TypeScript**: Type-safe JavaScript development 
- **SCSS**: Advanced CSS preprocessing

### Code Style & Conventions
- **C# 13.0**: Use latest language features
- **Nullable Reference Types**: Enabled project-wide
- **Implicit Usings**: Leverage global using statements
- **.editorconfig**: Use for consistent code style across IDEs

### Global Using Statements
The project uses extensive global using statements for common namespaces:

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

### GitHub Codespaces Support
The project is optimized for GitHub Codespaces development with pre-configured container support. 

### Best Practices

1. **Follow the established project structure**
2. **Use Bit.BlazorUI components** - leverage the framework's built-in components and utilities
3. **Follow nullable reference type conventions** - all new code should be nullable-aware
4. **Use dependency injection** - leverage the built-in DI container for service registration
5. **Implement proper logging** - use structured logging throughout the application
6. **Follow security best practices** - use proper authentication and authorization patterns
7. **Async Programming** - prefer async/await for I/O-bound operations, avoid blocking calls

### Tool Usage: DeepWiki for BitPlatform
You have a special tool, `deepwiki`, for accessing documentation about the `bitfoundation/bitplatform` library.

**RULE:** If a question mentions or implies the use of a "Bit" component (like `BitButton`, `BitTooltip`, `BitTextField`), or refers to the "bitplatform" or "bit Boilerplate",
you **MUST** use the `deepwiki` tool to find the correct implementation before writing any code. Do not use generic HTML/CSS/JS solutions for these components.

**Command:**
`deepwiki fetch bitfoundation/bitplatform`

**Example Usage:**
*   **User Prompt:** "How do I make a BitButton disabled?"
*   **Your Action:** First, run `deepwiki fetch bitfoundation/bitplatform How do I make a BitButton disabled?`. Then, use the documentation to answer the user.