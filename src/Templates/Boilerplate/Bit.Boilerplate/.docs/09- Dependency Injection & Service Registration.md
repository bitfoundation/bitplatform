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

1. **`ISharedServiceCollectionExtensions.cs`** ([`src/Shared/Extensions/`](/src/Shared/Extensions/ISharedServiceCollectionExtensions.cs))
   - Registers services used by **both server and client** projects
   - Core services like localization, authorization, configuration, and date/time providers
   - Contains the crucial `ConfigureAuthorizationCore()` method that defines authorization policies
   
   **Key Services Registered:**
   ```csharp
   services.AddScoped<HtmlRenderer>();
   services.AddScoped<CultureInfoManager>();
   services.AddScoped<IDateTimeProvider, DateTimeProvider>();
   services.AddSingleton<SharedSettings>();
   services.AddLocalization();
   services.AddMemoryCache();
   ```

2. **`IClientCoreServiceCollectionExtensions.cs`** ([`src/Client/Boilerplate.Client.Core/Extensions/`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs))
   - Registers services for **all client platforms** (Web, MAUI, Windows)
   - Services available during pre-rendering, Blazor Server, and Blazor WebAssembly
   - This is where most client-side infrastructure services are registered
   
   **Key Services Registered:**
   ```csharp
   services.AddScoped<ThemeService>();
   services.AddScoped<CultureService>();
   services.AddScoped<LazyAssemblyLoader>();
   services.AddScoped<IAuthTokenProvider, ClientSideAuthTokenProvider>();
   services.AddScoped<IExternalNavigationService, DefaultExternalNavigationService>();
   
   // Session-based services (Singleton in Hybrid, Scoped in Server/WASM)
   services.AddSessioned<PubSubService>();
   services.AddSessioned<PromptService>();
   services.AddSessioned<SnackBarService>();
   services.AddSessioned<AuthManager>();
   ```

3. **`Program.Services.cs`** files in each project
   - Platform-specific service registration
   - Each platform registers its own implementations of shared interfaces
   
   | File | Purpose |
   |------|---------|
   | [`Boilerplate.Server.Api/Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs) | API server services (DbContext, Identity, Email, SMS, Push Notifications, AI, Hangfire) |
   | [`Boilerplate.Server.Web/Program.Services.cs`](/src/Server/Boilerplate.Server.Web/Program.Services.cs) | Blazor Server/SSR services (combines API + Client services) |
   | [`Boilerplate.Client.Web/Program.Services.cs`](/src/Client/Boilerplate.Client.Web/Program.Services.cs) | Blazor WebAssembly-specific services |
   | [`Boilerplate.Client.Maui/MauiProgram.Services.cs`](/src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs) | MAUI services (Android, iOS, macOS, Windows via MAUI) |
   | [`Boilerplate.Client.Windows/Program.Services.cs`](/src/Client/Boilerplate.Client.Windows/Program.Services.cs) | Windows Forms Blazor Hybrid services |

4. **Platform-specific extensions** (MAUI only)
   - Each mobile/desktop platform can register its own specialized services
   
   | Extension File | Platform |
   |---------------|----------|
   | [`IAndroidServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Android/Extensions/IAndroidServiceCollectionExtensions.cs) | Android-specific services (e.g., `AndroidPushNotificationService`) |
   | [`IIosServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/iOS/Extensions/IIosServiceCollectionExtensions.cs) | iOS-specific services |
   | [`IMacServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs) | macOS-specific services |
   | [`IWindowsServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Windows/Extensions/IWindowsServiceCollectionExtensions.cs) | Windows (MAUI)-specific services |

## Service Registration Hierarchy

Services are registered in a hierarchical manner, with each layer building on the previous one:

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

### Registration Flow Example (Server API):

1. **`Program.cs`** calls `AddServerApiProjectServices()`
2. **`AddServerApiProjectServices()`** internally calls helper methods like `AddIdentity()`, `AddSwaggerGen()`
3. Each helper method registers related services (e.g., Identity services, JWT authentication, external auth providers)

## Service Availability Matrix

This matrix shows where each type of service registration is available:

| Registration Location | Web (WASM) | Web (Server/SSR) | MAUI (Android/iOS/Mac) | Windows Forms | Server API |
|----------------------|------------|------------------|------------------------|---------------|------------|
| `ISharedServiceCollectionExtensions` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `IClientCoreServiceCollectionExtensions` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `Program.Services.cs` (Client.Web) | ✅ | ✅ | ❌ | ❌ | ❌ |
| `MauiProgram.Services.cs` | ❌ | ❌ | ✅ | ❌ | ❌ |
| `Program.Services.cs` (Client.Windows) | ❌ | ❌ | ❌ | ✅ | ❌ |
| `Program.Services.cs` (Server.Api) | ❌ | ❌ | ❌ | ❌ | ✅ |
| `Program.Services.cs` (Server.Web) | ❌ | ✅ | ❌ | ❌ | ✅ (combined) |
| Platform-specific (Android) | ❌ | ❌ | ✅ (Android only) | ❌ | ❌ |
| Platform-specific (iOS) | ❌ | ❌ | ✅ (iOS only) | ❌ | ❌ |
| Platform-specific (Mac) | ❌ | ❌ | ✅ (Mac only) | ❌ | ❌ |
| Platform-specific (Windows/MAUI) | ❌ | ❌ | ✅ (Windows/MAUI only) | ❌ | ❌ |

**Note:** `Server.Web` gets **both** Server API services and Client Web services because it hosts Blazor Server/SSR, which needs both backend APIs and frontend components.

## The `AddSessioned` Method

One of the most important concepts in this project is the **`AddSessioned`** extension method, which intelligently registers services based on the application mode.

### What is AddSessioned?

`AddSessioned` is a custom extension method that automatically chooses the correct service lifetime based on the hosting environment:

- **Blazor Hybrid (MAUI, Windows Forms)**: Registers as `Singleton`
- **Blazor WebAssembly / Blazor Server**: Registers as `Scoped`

### Why is AddSessioned Needed?

Some services must be **unique per user session**, but the definition of "session" differs by platform:

- **Blazor Server**: Multiple users share the server, each with a SignalR circuit → Use `Scoped`
- **Blazor Hybrid**: Single-user desktop/mobile app → Use `Singleton` (no multiple users)
- **Blazor WebAssembly**: Single user in the browser → Use `Scoped` (per browser session)

**Critical Security Issue:** If you registered these services as `Singleton` in Blazor Server, **all users would share the same service instance**, causing data leakage and security issues! One user could see another user's authentication state, notifications, or personal data.

### AddSessioned Implementation

Located in [`IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs):

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

internal static IServiceCollection AddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
    where TService : class
{
    if (AppPlatform.IsBlazorHybrid)
    {
        services.Add(ServiceDescriptor.Singleton(implementationFactory));
    }
    else
    {
        services.Add(ServiceDescriptor.Scoped(implementationFactory));
    }

    return services;
}

internal static void AddSessioned<TService>(this IServiceCollection services)
    where TService : class
{
    if (AppPlatform.IsBlazorHybrid)
    {
        services.AddSingleton<TService, TService>();
    }
    else
    {
        services.AddScoped<TService, TService>();
    }
}
```

### Services Using AddSessioned

From [`IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs):

```csharp
// The following services must be unique to each app session.
// Defining them as singletons would result in them being shared across all users in Blazor Server and during pre-rendering.
// To address this, we use the AddSessioned extension method.
// AddSessioned applies AddSingleton in BlazorHybrid and AddScoped in Blazor WebAssembly and Blazor Server, 
// ensuring correct service lifetimes for each environment.

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

// SignalR HubConnection - must be per-session
services.AddSessioned(sp =>
{
    var authManager = sp.GetRequiredService<AuthManager>();
    var absoluteServerAddressProvider = sp.GetRequiredService<AbsoluteServerAddressProvider>();

    var hubConnection = new HubConnectionBuilder()
        .WithStatefulReconnect()
        .AddJsonProtocol(/* ... */)
        .WithAutomaticReconnect(sp.GetRequiredService<IRetryPolicy>())
        .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub"), options =>
        {
            options.SkipNegotiation = false;
            options.Transports = HttpTransportType.WebSockets;
            options.AccessTokenProvider = async () =>
            {
                return await authManager.GetFreshAccessToken(requestedBy: nameof(HubConnection));
            };
        })
        .Build();
    return hubConnection;
});
```

These services need to be per-session because they maintain user-specific state:
- **`PubSubService`**: User-specific pub/sub messages
- **`AuthManager`**: User's authentication state and tokens
- **`SnackBarService`**: User's notification queue
- **`HubConnection`**: User's SignalR connection to the server

## The `[AutoInject]` Attribute

The project uses the **`[AutoInject]`** attribute from the `Bit.SourceGenerators` package to simplify dependency injection.

### What is [AutoInject]?

`[AutoInject]` is a source generator attribute that automatically injects dependencies without needing constructor injection. It generates the necessary constructor code at compile time.

### Benefits of [AutoInject]

1. **Cleaner Code**: No need for constructor boilerplate
2. **Inheritance-Friendly**: Base class dependencies are automatically available in derived classes
3. **Reduces Repetition**: Don't need to re-declare dependencies that are already in base classes
4. **Better than Primary Constructors for Inheritance**: Unlike C# 12 primary constructors, `[AutoInject]` doesn't require passing base dependencies through constructor chains

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

**Important:** The class must be declared as `partial` for the source generator to work.

### Real Examples from the Project

**Example 1:** [`ServerSideAuthTokenProvider.cs`](/src/Server/Boilerplate.Server.Web/Services/ServerSideAuthTokenProvider.cs)

```csharp
public partial class ServerSideAuthTokenProvider : IAuthTokenProvider
{
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    public async Task<string?> GetAccessToken()
    {
        if (jsRuntime.IsInitialized())
        {
            return await storageService.GetItem("access_token");
        }

        return httpContextAccessor.HttpContext?.Request.Cookies["access_token"];
    }
}
```

**Example 2:** [`PhoneService.cs`](/src/Server/Boilerplate.Server.Api/Services/PhoneService.cs)

```csharp
public partial class PhoneService
{
    [AutoInject] private readonly ServerApiSettings appSettings = default!;
    [AutoInject] private readonly PhoneNumberUtil phoneNumberUtil = default!;
    [AutoInject] private readonly IHostEnvironment hostEnvironment = default!;
    [AutoInject] private readonly ILogger<PhoneService> phoneLogger = default!;
    [AutoInject] private readonly IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private readonly IBackgroundJobClient backgroundJobClient = default!;

    public virtual string? NormalizePhoneNumber(string? phoneNumber)
    {
        // Implementation uses all the injected services
        var region = httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("CF-IPCountry", out var value)
                         ? value.ToString()
                         : new RegionInfo(CultureInfo.CurrentUICulture.Name).TwoLetterISORegionName;

        var parsedPhoneNumber = phoneNumberUtil.Parse(phoneNumber, region);
        return phoneNumberUtil.Format(parsedPhoneNumber, PhoneNumberFormat.E164);
    }
}
```

### Important: [AutoInject] with Inheritance

The key advantage of `[AutoInject]` over primary constructors is **inheritance**. 

**Example:** [`AppComponentBase.cs`](/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs) has many injected services:

```csharp
public partial class AppComponentBase : ComponentBase, IAsyncDisposable
{
    [AutoInject] protected IJSRuntime JSRuntime = default!;
    [AutoInject] protected IStorageService StorageService = default!;
    [AutoInject] protected JsonSerializerOptions JsonSerializerOptions = default!;
    [AutoInject] protected IPrerenderStateService PrerenderStateService = default!;
    [AutoInject] protected PubSubService PubSubService = default!;
    [AutoInject] protected IConfiguration Configuration = default!;
    [AutoInject] protected NavigationManager NavigationManager = default!;
    [AutoInject] protected IAuthTokenProvider AuthTokenProvider = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected IExceptionHandler ExceptionHandler = default!;
    [AutoInject] protected AuthManager AuthManager = default!;
    [AutoInject] protected SnackBarService SnackBarService = default!;
    [AutoInject] protected ITelemetryContext TelemetryContext = default!;
    [AutoInject] protected IAuthorizationService AuthorizationService = default!;
    [AutoInject] protected AbsoluteServerAddressProvider AbsoluteServerAddress { get; set; } = default!;

    // Component lifecycle methods, WrapHandled methods, etc.
}
```

**With [AutoInject]**: Any child component that inherits from `AppComponentBase` automatically has access to **all** these services without needing to redeclare them.

```csharp
public partial class ProductPage : AppPageBase // AppPageBase inherits from AppComponentBase
{
    // Automatically has access to:
    // - NavigationManager
    // - Localizer
    // - AuthManager
    // - SnackBarService
    // - All other services from AppComponentBase
    
    // No need to inject them again!
}
```

**With Primary Constructors**: You would need to pass all base dependencies through the constructor chain, leading to extremely verbose code:

```csharp
// This would be required with primary constructors - NOT with [AutoInject]!
public partial class ProductPage(
    IJSRuntime jsRuntime,
    IStorageService storageService,
    NavigationManager navigationManager,
    IStringLocalizer<AppStrings> localizer,
    // ... 10+ more parameters from AppComponentBase
    IProductService productService // Your own service
) : AppPageBase(jsRuntime, storageService, navigationManager, localizer, /* ... */)
{
    // This is why [AutoInject] is superior for inheritance scenarios
}
```

### [AutoInject] in Controllers

Controllers also benefit from `[AutoInject]`. [`AppControllerBase.cs`](/src/Server/Boilerplate.Server.Api/Controllers/AppControllerBase.cs):

```csharp
public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;
    [AutoInject] protected AppDbContext DbContext = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
}
```

Any controller that inherits from `AppControllerBase` automatically has access to `AppSettings`, `DbContext`, and `Localizer` without needing to inject them manually.

## Key Rules for Service Registration

### 1. Register Services in the Appropriate Location

- **Shared by all projects?** → `ISharedServiceCollectionExtensions.cs`
- **Client-side only (all platforms)?** → `IClientCoreServiceCollectionExtensions.cs`
- **Blazor Web (WASM + Server)?** → `Program.Services.cs` in `Client.Web`
- **MAUI (all mobile platforms)?** → `MauiProgram.Services.cs`
- **Platform-specific (Android/iOS/Mac/Windows)?** → Corresponding platform-specific extension file
- **Server API only?** → `Program.Services.cs` in `Server.Api`
- **Server Web (Blazor Server/SSR)?** → `Program.Services.cs` in `Server.Web`

### 2. Use Correct Service Lifetimes

- **Singleton**: Created once per application lifetime
  - Use for: Stateless services, configuration objects, caches, utilities
  - Example: `HtmlSanitizer`, `PhoneNumberUtil`, `IBlobStorage`, `SharedSettings`
  - **Warning:** In Blazor Server, singletons are shared across **all users**

- **Scoped**: Created once per request (HTTP request or SignalR circuit)
  - Use for: Services that maintain per-request state
  - Example: `AppDbContext`, `EmailService`, controllers, user-specific services
  - **Blazor Server:** One scope per SignalR circuit (per user)
  - **Blazor WebAssembly:** One scope per browser session
  - **API Request:** One scope per HTTP request

- **Transient**: Created every time they are requested
  - Use for: Lightweight, stateless services with minimal overhead
  - Example: `IPrerenderStateService`
  - **Warning:** Avoid for heavyweight services (DbContext, HttpClient, etc.)

- **Sessioned** (Custom): Singleton in Blazor Hybrid, Scoped elsewhere
  - Use for: Services that need per-user-session state but must adapt to the hosting environment
  - Example: `AuthManager`, `PubSubService`, `SnackBarService`, `HubConnection`
  - **Why needed:** Prevents data leakage in Blazor Server while optimizing for single-user Hybrid apps

### 3. Be Careful with Blazor Server

In Blazor Server, **Singleton services are shared across ALL users**. Never store user-specific data in Singleton services unless properly scoped (e.g., using `IHttpContextAccessor` or `AsyncLocal<T>`).

**Example of WRONG usage:**
```csharp
// DON'T DO THIS in Blazor Server!
public class UserSessionService // Registered as Singleton
{
    public string CurrentUserId { get; set; } // All users would see the same value!
}
```

**Example of CORRECT usage:**
```csharp
// Use Scoped or AddSessioned instead
services.AddScoped<UserSessionService>(); // Each SignalR circuit gets its own instance
// OR
services.AddSessioned<UserSessionService>(); // Scoped in Server, Singleton in Hybrid
```

### 4. Platform-Specific Implementations

When you need different implementations per platform (e.g., `IPushNotificationService`):

1. **Define the interface** in `Boilerplate.Shared` or `Boilerplate.Client.Core`
2. **Create platform-specific implementations** in each platform project
3. **Register the appropriate implementation** in each platform's service registration file

**Example:**

| Platform | Implementation | Registered In |
|----------|---------------|---------------|
| Android | `AndroidPushNotificationService` | `IAndroidServiceCollectionExtensions.cs` |
| iOS | `IosPushNotificationService` | `IIosServiceCollectionExtensions.cs` |
| Web | `WebPushNotificationService` | `Program.Services.cs` (Client.Web) |
| Windows Forms | `WindowsPushNotificationService` | `Program.Services.cs` (Client.Windows) |

This allows each platform to provide its own native implementation while sharing the same interface.

## Example: Adding a New Service

Let's say you want to add a `ProductSyncService` that works on all client platforms:

### Step 1: Create the Service

```csharp
// src/Client/Boilerplate.Client.Core/Services/ProductSyncService.cs
namespace Boilerplate.Client.Core.Services;

public partial class ProductSyncService
{
    [AutoInject] private IProductController productController = default!;
    [AutoInject] private OfflineDbContext offlineDb = default!;
    [AutoInject] private ILogger<ProductSyncService> logger = default!;

    public async Task SyncProductsAsync()
    {
        logger.LogInformation("Starting product sync...");
        
        var products = await productController.GetProducts();
        
        // Save to offline database
        await offlineDb.Products.AddRangeAsync(products);
        await offlineDb.SaveChangesAsync();
        
        logger.LogInformation("Product sync completed. Synced {Count} products.", products.Count);
    }
}
```

### Step 2: Register the Service

```csharp
// src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs
public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing services ...
    
    // Register as Sessioned because sync state should be per-user
    services.AddSessioned<ProductSyncService>();
    
    return services;
}
```

### Step 3: Use the Service in a Component

```csharp
// In any component or page
public partial class ProductPage : AppPageBase
{
    [AutoInject] private ProductSyncService productSyncService = default!;

    protected override async Task OnInitAsync()
    {
        try
        {
            await productSyncService.SyncProductsAsync();
            SnackBarService.Success(Localizer[nameof(AppStrings.ProductsSyncedSuccessfully)]);
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
    }
}
```

**Note:** The component automatically has access to `SnackBarService`, `Localizer`, and `ExceptionHandler` from `AppComponentBase` without needing to inject them explicitly!

## Summary

The Dependency Injection architecture in this project is designed for:

1. **Flexibility**: Services can be registered at the appropriate level (shared, client, platform-specific, server)
2. **Safety**: `AddSessioned` ensures proper service lifetimes across different hosting models, preventing data leakage
3. **Simplicity**: `[AutoInject]` reduces boilerplate and improves code readability, especially with inheritance
4. **Maintainability**: Clear separation of concerns with dedicated service registration files for each project and platform
5. **Performance**: Proper service lifetimes ensure optimal memory usage and prevent unnecessary object creation

Understanding where and how to register services is crucial for extending the application and maintaining proper separation between server and client, as well as between different client platforms.

### Quick Reference Guide

**Where should I register my service?**

- All projects (server + client) → `ISharedServiceCollectionExtensions.cs`
- All client platforms → `IClientCoreServiceCollectionExtensions.cs`
- Only Blazor Web → `Client.Web/Program.Services.cs`
- Only MAUI apps → `MauiProgram.Services.cs`
- Only Android → `IAndroidServiceCollectionExtensions.cs`
- Only Server API → `Server.Api/Program.Services.cs`

**What lifetime should I use?**

- Configuration, utilities, caches → `Singleton`
- DbContext, per-request services → `Scoped`
- Lightweight, stateless → `Transient`
- Per-user session (adapts to hosting) → `AddSessioned`

**How should I inject dependencies?**

- In components/pages → Use `[AutoInject]` and inherit from `AppComponentBase`
- In services → Use `[AutoInject]` (make class `partial`)
- In controllers → Use `[AutoInject]` and inherit from `AppControllerBase`

---