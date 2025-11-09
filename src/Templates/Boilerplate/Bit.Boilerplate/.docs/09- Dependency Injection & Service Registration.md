# Stage 9: Dependency Injection & Service Registration

This stage explains how Dependency Injection (DI) is structured in the Boilerplate project and where services are registered for different platforms and environments.

## Overview

The project follows a **modular service registration pattern** where services are registered in different extension methods based on their scope and applicability. This approach ensures:

- **Separation of concerns**: Each project registers only the services it needs
- **Platform-specific implementations**: Different platforms can provide their own service implementations
- **Shared services**: Common services are registered once and available everywhere
- **Clear service lifetimes**: Proper service lifetimes (Singleton, Scoped, Transient) for each environment

## Service Registration Architecture

### Extension Method Files

Service registration is organized through `*ServiceCollectionExtensions.cs` and `*.Services.cs` files throughout the solution:

1. **`ISharedServiceCollectionExtensions.cs`** (`src/Shared/Extensions/`)
   - Registers services used by **both server and client** projects
   - Core services like localization, authorization, and configuration
   - Example: [`ISharedServiceCollectionExtensions.cs`](/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs)

2. **`IClientCoreServiceCollectionExtensions.cs`** (`src/Client/Boilerplate.Client.Core/Extensions/`)
   - Registers services for **all client platforms** (Web, MAUI, Windows)
   - Services available during pre-rendering, Blazor Server, and Blazor WebAssembly
   - Example: [`IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)

3. **`Program.Services.cs`** files in each project
   - Platform-specific service registration
   - [`Boilerplate.Server.Api/Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs) - API server services
   - [`Boilerplate.Server.Web/Program.Services.cs`](/src/Server/Boilerplate.Server.Web/Program.Services.cs) - Blazor Server/SSR services
   - [`Boilerplate.Client.Web/Program.Services.cs`](/src/Client/Boilerplate.Client.Web/Program.Services.cs) - Blazor WebAssembly services
   - [`Boilerplate.Client.Maui/MauiProgram.Services.cs`](/src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs) - MAUI services

4. **Platform-specific extensions** (MAUI only)
   - [`IAndroidServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Android/Extensions/IAndroidServiceCollectionExtensions.cs)
   - [`IIosServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/iOS/Extensions/IIosServiceCollectionExtensions.cs)
   - [`IMacServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs)
   - [`IWindowsServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Windows/Extensions/IWindowsServiceCollectionExtensions.cs)

## Service Registration Hierarchy

Services are registered in a hierarchical manner:

```
Program.Services.cs (Platform-specific)
    ↓
IClientCoreServiceCollectionExtensions.cs (All Clients)
    ↓
ISharedServiceCollectionExtensions.cs (Server + Client)
```

### Registration Flow Example (Blazor WebAssembly):

1. **`Program.cs`** calls `ConfigureServices()`
2. **`Program.Services.cs`** calls `AddClientWebProjectServices()`
3. **`AddClientWebProjectServices()`** calls `AddClientCoreProjectServices()`
4. **`AddClientCoreProjectServices()`** calls `AddSharedProjectServices()`

Each layer adds services specific to its scope, ensuring proper service availability across the application.

## Service Availability Matrix

| Registration Location | Web (WASM) | Web (Server/SSR) | MAUI (Android/iOS) | Windows | Server API |
|----------------------|------------|------------------|-------------------|---------|------------|
| `ISharedServiceCollectionExtensions` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IClientCoreServiceCollectionExtensions` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `Program.Services.cs` (Client.Web) | ✅ | ✅ | ❌ | ❌ | ❌ |
| `MauiProgram.Services.cs` | ❌ | ❌ | ✅ | ❌ | ❌ |
| `Program.Services.cs` (Client.Windows) | ❌ | ❌ | ❌ | ✅ | ❌ |
| `Program.Services.cs` (Server.Api) | ❌ | ❌ | ❌ | ❌ | ✅ |
| `Program.Services.cs` (Server.Web) | ❌ | ✅ | ❌ | ❌ | ✅ |
| Platform-specific (Android/iOS/Mac/Windows) | ❌ | ❌ | ✅ (per platform) | ❌ | ❌ |

## The `AddSessioned` Method

One of the most important concepts in this project is the **`AddSessioned`** extension method, which intelligently registers services based on the application mode.

### What is AddSessioned?

`AddSessioned` is a custom extension method that automatically chooses the correct service lifetime based on the hosting environment:

- **Blazor Hybrid (MAUI, Windows)**: Registers as `Singleton`
- **Blazor WebAssembly / Blazor Server**: Registers as `Scoped`

### Why is AddSessioned Needed?

Some services must be **unique per user session**, but the definition of "session" differs by platform:

- **Blazor Server**: Multiple users share the server, each with a SignalR circuit → Use `Scoped`
- **Blazor Hybrid**: Single-user desktop/mobile app → Use `Singleton` (no multiple users)

If you registered these services as `Singleton` in Blazor Server, **all users would share the same service instance**, causing data leakage and security issues!

### AddSessioned Implementation

```csharp
internal static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
    where TImplementation : class, TService
    where TService : class
{
    if (AppPlatform.IsBlazorHybrid)
    {
        return services.AddSingleton<TService, TImplementation>();
    }
    else
    {
        return services.AddScoped<TService, TImplementation>();
    }
}
```

### Services Using AddSessioned

From [`IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs):

```csharp
services.AddSessioned<PubSubService>();
services.AddSessioned<PromptService>();
services.AddSessioned<SnackBarService>();
services.AddSessioned<ILocalHttpServer, NoOpLocalHttpServer>();
services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
services.AddSessioned<AuthenticationStateProvider>(sp =>
{
    var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
    authenticationStateProvider.OnInit();
    return authenticationStateProvider;
});
services.AddSessioned(sp => (AuthManager)sp.GetRequiredService<AuthenticationStateProvider>());
services.AddSessioned(sp => /* HubConnection setup */);
```

These services need to be per-session because they maintain user-specific state (authentication, notifications, pub/sub messages, etc.).

## The `[AutoInject]` Attribute

The project uses the **`[AutoInject]`** attribute from the `Bit.SourceGenerators` package to simplify dependency injection.

### What is [AutoInject]?

`[AutoInject]` is a source generator attribute that automatically injects dependencies without needing constructor injection. It works similarly to C# primary constructors but with additional benefits.

### Benefits of [AutoInject]

1. **Cleaner Code**: No need for constructor boilerplate
2. **Inheritance-Friendly**: Base class dependencies are automatically available in derived classes
3. **Reduces Repetition**: Don't need to re-declare dependencies that are already in base classes

### Example Usage

**Without [AutoInject] (Traditional Constructor Injection):**

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public MyService(ILogger<MyService> logger, IConfiguration configuration, AppDbContext dbContext)
    {
        _logger = logger;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public void DoWork()
    {
        _logger.LogInformation("Working...");
    }
}
```

**With [AutoInject]:**

```csharp
public partial class MyService
{
    [AutoInject] private ILogger<MyService> logger = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private AppDbContext dbContext = default!;

    public void DoWork()
    {
        logger.LogInformation("Working...");
    }
}
```

### Real Examples from the Project

From [`ServerSideAuthTokenProvider.cs`](/src/Server/Boilerplate.Server.Web/Services/ServerSideAuthTokenProvider.cs):

```csharp
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    // Methods can directly use these injected dependencies
}
```

From [`WindowsLocalHttpServer.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsLocalHttpServer.cs):

```csharp
public partial class WindowsLocalHttpServer : ILocalHttpServer
{
    [AutoInject] private PubSubService pubSubService;
    [AutoInject] private IExceptionHandler exceptionHandler;
    [AutoInject] private ClientWindowsSettings clientWindowsSettings;
    [AutoInject] private AbsoluteServerAddressProvider absoluteServerAddressProvider;

    // All dependencies are automatically injected and ready to use
}
```

### Important: [AutoInject] with Inheritance

The key advantage of `[AutoInject]` over primary constructors is **inheritance**. 

For example, `AppComponentBase` has many injected services:
- `NavigationManager`
- `IStringLocalizer`
- `IExceptionHandler`
- `CurrentCancellationToken`
- And more...

**With [AutoInject]**: Child components automatically have access to all base class dependencies without needing to redeclare them.

**With Primary Constructors**: You would need to pass all base dependencies through the constructor chain, leading to very verbose code.

## Key Rules for Service Registration

### 1. Register Services in the Appropriate Location

- **Shared by all projects?** → `ISharedServiceCollectionExtensions.cs`
- **Client-side only (all platforms)?** → `IClientCoreServiceCollectionExtensions.cs`
- **Platform-specific?** → Platform's `Program.Services.cs` or platform-specific extension
- **Server API only?** → `Boilerplate.Server.Api/Program.Services.cs`

### 2. Use Correct Service Lifetimes

- **Singleton**: Created once per application lifetime
  - Use for: Stateless services, configuration, caches
  - Example: `HtmlSanitizer`, `PhoneNumberUtil`, `IBlobStorage`

- **Scoped**: Created once per request (HTTP request or SignalR circuit)
  - Use for: Services that maintain per-request state
  - Example: `AppDbContext`, `EmailService`, controllers

- **Transient**: Created every time they are requested
  - Use for: Lightweight, stateless services
  - Example: `IPrerenderStateService`

- **Sessioned**: Singleton in Blazor Hybrid, Scoped elsewhere
  - Use for: Services that need per-user-session state
  - Example: `AuthManager`, `PubSubService`, `HubConnection`

### 3. Be Careful with Blazor Server

In Blazor Server, **Singleton services are shared across ALL users**. Never store user-specific data in Singleton services unless properly scoped (e.g., using `IHttpContextAccessor` or `AsyncLocal<T>`).

### 4. Platform-Specific Implementations

When you need different implementations per platform (e.g., `IPushNotificationService`):

1. Define the interface in `Boilerplate.Shared` or `Boilerplate.Client.Core`
2. Create platform-specific implementations
3. Register the appropriate implementation in each platform's service registration

Example:
- `AndroidPushNotificationService` → Registered in `IAndroidServiceCollectionExtensions.cs`
- `WebPushNotificationService` → Registered in `Program.Services.cs` (Client.Web)
- `MauiAppUpdateService` → Registered in `MauiProgram.Services.cs`

## Example: Adding a New Service

Let's say you want to add a `ProductSyncService` that works on all client platforms:

### Step 1: Create the Service

```csharp
// src/Client/Boilerplate.Client.Core/Services/ProductSyncService.cs
public partial class ProductSyncService
{
    [AutoInject] private IProductController productController = default!;
    [AutoInject] private IOfflineDbContext offlineDb = default!;
    [AutoInject] private ILogger<ProductSyncService> logger = default!;

    public async Task SyncProductsAsync()
    {
        var products = await productController.GetProducts();
        // Sync logic...
    }
}
```

### Step 2: Register the Service

```csharp
// src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs
public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing services ...
    
    services.AddSessioned<ProductSyncService>(); // Per-user service
    
    return services;
}
```

### Step 3: Use the Service

```csharp
// In any component or page
public partial class ProductPage
{
    [AutoInject] private ProductSyncService productSyncService = default!;

    protected override async Task OnInitAsync()
    {
        await productSyncService.SyncProductsAsync();
    }
}
```

## Summary

The Dependency Injection architecture in this project is designed for:

1. **Flexibility**: Services can be registered at the appropriate level (shared, client, platform-specific)
2. **Safety**: `AddSessioned` ensures proper service lifetimes across different hosting models
3. **Simplicity**: `[AutoInject]` reduces boilerplate and improves code readability
4. **Maintainability**: Clear separation of concerns with dedicated service registration files

Understanding where and how to register services is crucial for extending the application and maintaining proper separation between server and client, as well as between different client platforms.

---