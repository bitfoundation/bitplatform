# Stage 9: Dependency Injection & Service Registration

This stage explains how Dependency Injection (DI) is structured in the Boilerplate project and where services are registered for different platforms and environments.

## Overview

The project follows a **modular service registration pattern** where services are registered in different extension methods based on their scope and applicability. This approach ensures:

- **Separation of concerns**: Each project registers only the services it needs
- **Platform-specific implementations**: Different platforms can provide their own service implementations
- **Shared services**: Common services are registered once and available everywhere

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
2. **`AddServerApiProjectServices()`** internally calls helper methods like `AddIdentity()`
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

Some services must be **unique per user session / client app**, but the definition of "session" differs by platform:

 -  Blazor Server creates one scope for each connected user's client app.
 -  Blazor WebAssembly has only one scope for the client app.
 -  MAUI + Blazor Hybrid would create two scopes for the client app: one for the native part and one for the Blazor's WebView.
 -  In Blazor Hybrid, we register sessioned services as singletons to share them between the two scopes.
 -  In Blazor Server we **MUST** register sessioned services as scoped to avoid sharing them between different users.
 -  In Blazor WebAssembly it doesn't matter if we register sessioned services as scoped or singleton because there's only one scope per client app.

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
```

These services need to be per-session because they maintain client-specific state:
- **`PubSubService`**: Client app specific pub/sub messages
- **`AuthManager`**: Client app's user's authentication state and tokens

## Key Rules for Service Registration

### 1. Platform-Specific Implementations

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