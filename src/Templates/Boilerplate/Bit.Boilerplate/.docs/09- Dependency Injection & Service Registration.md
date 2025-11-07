# Stage 9: Dependency Injection & Service Registration

Welcome to Stage 9! In this stage, we'll explore how the project organizes dependency injection (DI) and service registration across different platforms and layers.

## Overview

This project uses a **layered service registration architecture** where services are registered in different extension methods based on where they can be used. This ensures:

- **Clear separation of concerns**: Services are only available where they're actually needed
- **Platform-specific implementations**: Each platform can have its own service implementations
- **Shared services**: Common services are registered once and available everywhere
- **Type safety**: Compile-time guarantees about what services are available where

## Service Registration Files

The project uses extension method classes named `*ServiceCollectionExtensions.cs` and `*.Services.cs` to organize DI registrations:

### Shared Layer
- **[`ISharedServiceCollectionExtensions.cs`](../src/Shared/Extensions/ISharedServiceCollectionExtensions.cs)** - Services available **everywhere** (server, web, MAUI, Windows)

### Client Core Layer
- **[`IClientCoreServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)** - Services available in **all client platforms** (WebAssembly, Server pre-rendering, MAUI, Windows)

### Platform-Specific Layers
- **[`Program.Services.cs` (Client.Web)](../src/Client/Boilerplate.Client.Web/Program.Services.cs)** - WebAssembly + Server pre-rendering services
- **[`MauiProgram.Services.cs`](../src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs)** - MAUI cross-platform services
- **[`Program.Services.cs` (Client.Windows)](../src/Client/Boilerplate.Client.Windows/Program.Services.cs)** - Windows Forms services
- **[`IAndroidServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Maui/Platforms/Android/Extensions/IAndroidServiceCollectionExtensions.cs)** - Android-only services
- **[`IIosServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Maui/Platforms/iOS/Extensions/IIosServiceCollectionExtensions.cs)** - iOS-only services
- **[`IMacServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Extensions/IMacServiceCollectionExtensions.cs)** - macOS-only services
- **[`IWindowsServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Maui/Platforms/Windows/Extensions/IWindowsServiceCollectionExtensions.cs)** - MAUI Windows-only services

### Server Layer
- **[`WebApplicationBuilderExtensions.cs`](../src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs)** - Shared server services (API + Web)
- **[`Program.Services.cs` (Server.Api)](../src/Server/Boilerplate.Server.Api/Program.Services.cs)** - API server services
- **[`Program.Services.cs` (Server.Web)](../src/Server/Boilerplate.Server.Web/Program.Services.cs)** - Web server services

## Registration Call Hierarchy

Here's how these registration methods call each other, forming a hierarchical structure:

```
ISharedServiceCollectionExtensions (Base - Available Everywhere)
    └─> IClientCoreServiceCollectionExtensions (All Clients)
        ├─> Program.Services.cs (Client.Web) - WebAssembly & Server Blazor
        ├─> MauiProgram.Services.cs - MAUI cross-platform
        │   ├─> IAndroidServiceCollectionExtensions - Android
        │   ├─> IIosServiceCollectionExtensions - iOS
        │   ├─> IMacServiceCollectionExtensions - macOS
        │   └─> IWindowsServiceCollectionExtensions - MAUI Windows
        └─> Program.Services.cs (Client.Windows) - Windows Forms

ISharedServiceCollectionExtensions (Base - Available Everywhere)
    └─> WebApplicationBuilderExtensions (Server Shared)
        ├─> Program.Services.cs (Server.Api) - API Services
        └─> Program.Services.cs (Server.Web) - Web Server + Blazor Server
            └─> (Can also call Server.Api services in Integrated mode)
```

## Service Availability Matrix

This matrix shows where each registration location's services are available:

| Registration Location | Blazor WASM | Blazor Server | MAUI Android | MAUI iOS | MAUI macOS | MAUI Windows | Windows Forms | Server API |
|----------------------|-------------|---------------|--------------|----------|------------|--------------|---------------|------------|
| **ISharedServiceCollectionExtensions** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **IClientCoreServiceCollectionExtensions** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Program.Services.cs (Client.Web)** | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **MauiProgram.Services.cs** | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **IAndroidServiceCollectionExtensions** | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **IIosServiceCollectionExtensions** | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| **IMacServiceCollectionExtensions** | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| **IWindowsServiceCollectionExtensions (MAUI)** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| **Program.Services.cs (Client.Windows)** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| **WebApplicationBuilderExtensions** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Program.Services.cs (Server.Api)** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Program.Services.cs (Server.Web)** | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

## Understanding `AddSessioned` - The Key Innovation

One of the most important patterns in this project is the `AddSessioned` extension method, defined in [`IClientCoreServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs):

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

### Why `AddSessioned` Exists

In Blazor, different hosting models have different service lifetime requirements:

- **Blazor WebAssembly**: Each user has their own instance of the app in their browser → `Scoped` and `Singleton` are effectively the same
- **Blazor Server**: Multiple users share the same server process → `Singleton` would share data between ALL users (security/privacy issue!)
- **Blazor Hybrid (MAUI, Windows Forms)**: Each user has their own native app → `Singleton` is safe and more efficient

`AddSessioned` automatically chooses the right lifetime based on the platform:

- **Blazor Hybrid** (MAUI/Windows): → `Singleton` (more efficient, one instance per app)
- **Blazor WASM/Server**: → `Scoped` (safe, isolated per user session)

### Real Examples from the Project

Let's look at how `AddSessioned` is used for critical services in [`IClientCoreServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs):

```csharp
// ✅ CORRECT: These services must be unique per user session
services.AddSessioned<PubSubService>();
services.AddSessioned<PromptService>();
services.AddSessioned<SnackBarService>();
services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
services.AddSessioned<AuthenticationStateProvider>(sp =>
{
    var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
    authenticationStateProvider.OnInit();
    return authenticationStateProvider;
});
```

**Why this matters**: These services track user-specific state:
- `AuthManager` - contains the current user's authentication status and tokens
- `PubSubService` - manages user-specific event subscriptions
- `SnackBarService` - shows notifications to a specific user

**What would happen if we used `AddSingleton` instead?**
- In **Blazor Server**: User A's authentication state would leak to User B! 🔥 Major security vulnerability
- In **MAUI/Windows**: Works fine, but `AddSessioned` makes the code safe everywhere

**What would happen if we used `AddScoped` everywhere?**
- In **Blazor Server/WASM**: Works correctly
- In **MAUI/Windows**: Works, but creates unnecessary new instances on every page navigation (less efficient)

### When to Use `AddSessioned` vs Standard Lifetimes

| Use Case | Recommended Approach | Example |
|----------|---------------------|---------|
| **User-specific state** (auth, user preferences, notifications) | `AddSessioned` | `AuthManager`, `SnackBarService` |
| **True stateless services** (calculations, utilities) | `AddScoped` | `DateTimeProvider`, `CultureInfoManager` |
| **Configuration/Settings** (read-only data) | `AddSingleton` | `ClientCoreSettings`, `SharedSettings` |
| **Expensive to create** (HTTP clients, DB contexts) | `AddScoped` (HTTP) or `AddDbContextPool` (EF) | `HttpClient`, `AppDbContext` |
| **Platform API wrappers** | `AddScoped` (client) or `AddSingleton` (server) | `IStorageService`, `IBitDeviceCoordinator` |

## Key Rules for Service Registration

### 1. **Register services in the most appropriate layer**

- **Shared everywhere?** → `ISharedServiceCollectionExtensions`
- **All clients need it?** → `IClientCoreServiceCollectionExtensions`
- **Only one platform?** → Platform-specific extension class

### 2. **Use the right service lifetime**

- **Stateless/Calculations**: `AddScoped` or `AddTransient`
- **Configuration/Settings**: `AddSingleton`
- **User-specific state**: `AddSessioned` (for cross-platform code)
- **HTTP clients**: `AddScoped` with `HttpMessageHandlersChainFactory`

### 3. **Never register the same service in multiple layers**

❌ **WRONG** - This would register `IDateTimeProvider` twice:

```csharp
// In ISharedServiceCollectionExtensions.cs
services.AddScoped<IDateTimeProvider, DateTimeProvider>();

// In IClientCoreServiceCollectionExtensions.cs
services.AddScoped<IDateTimeProvider, DateTimeProvider>(); // ❌ Duplicate!
```

✅ **CORRECT** - Register once in the appropriate layer:

```csharp
// In ISharedServiceCollectionExtensions.cs - available everywhere
services.AddScoped<IDateTimeProvider, DateTimeProvider>(); // ✅ Only once
```

### 4. **Use constructor injection with `[AutoInject]` attribute**

This project uses the `[AutoInject]` attribute from `Bit.SourceGenerators` to simplify dependency injection:

✅ **CORRECT** - Modern approach with `[AutoInject]`:

```csharp
public partial class ProductService
{
    [AutoInject] private HttpClient httpClient;
    [AutoInject] private ILocalizer<AppStrings> localizer;

    public async Task<ProductDto[]> GetProducts()
    {
        return await httpClient.GetFromJsonAsync<ProductDto[]>("api/products");
    }
}
```

**Benefits of `[AutoInject]`**:
- Less boilerplate code (no constructor needed)
- Services from base classes (like `AppComponentBase`) are automatically available in derived classes
- Type-safe at compile time
- Works in controllers, components, and regular classes

📖 **Learn more**: See the [Dependency Injection documentation](https://bitplatform.dev/bswup/di) for detailed information about `[AutoInject]`.

## Practical Examples

### Example 1: Registering a New Shared Service

Let's say you want to add a `WeatherService` that should be available everywhere:

**Step 1**: Create the service in `Shared/Services/`:

```csharp
// File: src/Shared/Services/WeatherService.cs
namespace Boilerplate.Shared.Services;

public partial class WeatherService
{
    [AutoInject] private HttpClient httpClient;

    public async Task<WeatherForecast> GetForecast(string city)
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast>($"api/weather/{city}");
    }
}
```

**Step 2**: Register it in `ISharedServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddSharedProjectServices(this IServiceCollection services, IConfiguration configuration)
{
    // Existing registrations...
    services.AddScoped<IDateTimeProvider, DateTimeProvider>();

    // ✅ Add your new service here
    services.AddScoped<WeatherService>();

    // ...rest of the method
    return services;
}
```

**Step 3**: Use it anywhere with `[AutoInject]`:

```csharp
public partial class WeatherPage : AppComponentBase
{
    [AutoInject] private WeatherService weatherService;

    protected override async Task OnInitAsync()
    {
        var forecast = await weatherService.GetForecast("Seattle");
    }
}
```

### Example 2: Platform-Specific Service Implementation

Let's add a `IShareService` that works differently on each platform:

**Step 1**: Define the interface in `Shared/Services/Contracts/`:

```csharp
// File: src/Shared/Services/Contracts/IShareService.cs
namespace Boilerplate.Shared.Services.Contracts;

public interface IShareService
{
    Task ShareText(string title, string text);
}
```

**Step 2**: Implement for each platform:

```csharp
// File: src/Client/Boilerplate.Client.Web/Services/WebShareService.cs
public class WebShareService : IShareService
{
    [AutoInject] private IJSRuntime jsRuntime;

    public async Task ShareText(string title, string text)
    {
        await jsRuntime.InvokeVoidAsync("navigator.share", new { title, text });
    }
}

// File: src/Client/Boilerplate.Client.Maui/Services/MauiShareService.cs
public class MauiShareService : IShareService
{
    public async Task ShareText(string title, string text)
    {
        await Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(
            new ShareTextRequest { Title = title, Text = text });
    }
}
```

**Step 3**: Register in each platform's services file:

```csharp
// In src/Client/Boilerplate.Client.Web/Program.Services.cs
services.AddScoped<IShareService, WebShareService>();

// In src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs
services.AddScoped<IShareService, MauiShareService>();

// In src/Client/Boilerplate.Client.Windows/Program.Services.cs
services.AddScoped<IShareService, WindowsShareService>();
```

**Step 4**: Use the same interface everywhere:

```csharp
public partial class ProductDetailPage : AppComponentBase
{
    [AutoInject] private IShareService shareService;

    private async Task ShareProduct(ProductDto product)
    {
        await shareService.ShareText(product.Name, product.Description);
    }
}
```

The correct platform-specific implementation will be injected automatically! 🎉

### Example 3: Adding a MAUI Android-Only Service

Suppose you need a service that only works on Android (e.g., accessing Android-specific APIs):

**Step 1**: Create the Android-specific service:

```csharp
// File: src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/AndroidSpecificService.cs
#if ANDROID
namespace Boilerplate.Client.Maui.Platforms.Android.Services;

public class AndroidSpecificService
{
    public string GetDeviceModel()
    {
        return Android.OS.Build.Model ?? "Unknown";
    }
}
#endif
```

**Step 2**: Register it in `IAndroidServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddClientMauiProjectAndroidServices(
    this IServiceCollection services, IConfiguration configuration)
{
    // Existing Android-only services...
    
    // ✅ Add your Android-only service
    services.AddSingleton<AndroidSpecificService>();

    return services;
}
```

**Step 3**: Use it in Android-specific code:

```csharp
#if ANDROID
public partial class DeviceInfoPage : AppComponentBase
{
    [AutoInject] private AndroidSpecificService androidService;

    protected override async Task OnInitAsync()
    {
        var model = androidService.GetDeviceModel();
        // Use the Android-specific information
    }
}
#endif
```

## Common Patterns in the Project

### 1. HttpClient Configuration

The project uses a sophisticated `HttpMessageHandlersChainFactory` to configure `HttpClient` with:
- **Authentication** (via `AuthDelegatingHandler`)
- **Logging** (via `LoggingDelegatingHandler`)
- **Retry logic** (via `RetryDelegatingHandler`)
- **Caching** (via `CacheDelegatingHandler`)
- **Request headers** (via `RequestHeadersDelegatingHandler`)

This is registered in [`IClientCoreServiceCollectionExtensions.cs`](../src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs) and used in all platform-specific `HttpClient` registrations.

### 2. Settings/Configuration Objects

Configuration is bound to strongly-typed settings classes and registered as singletons:

```csharp
services.AddSingleton(sp =>
{
    ClientCoreSettings settings = new();
    configuration.Bind(settings);
    return settings;
});

services.AddOptions<ClientCoreSettings>()
    .Bind(configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

This pattern is used in:
- `SharedSettings` (available everywhere)
- `ClientCoreSettings` (all clients)
- `ServerApiSettings` (API server)
- `ClientWebSettings` (Web client)
- `ClientMauiSettings` (MAUI apps)
- `ClientWindowsSettings` (Windows Forms)

### 3. JSON Serialization Configuration

Each layer adds its JSON context to support source-generated JSON serialization:

```csharp
services.TryAddSingleton(sp =>
{
    JsonSerializerOptions options = new JsonSerializerOptions(AppJsonContext.Default.Options);
    options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);
    return options;
});
```

This ensures efficient, AOT-compatible JSON serialization across all platforms.

### 4. Authentication Services

The project has a sophisticated authentication setup with different providers per platform:

```csharp
services.AddSessioned<AuthenticationStateProvider>(sp =>
{
    var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
    authenticationStateProvider.OnInit();
    return authenticationStateProvider;
});
services.AddSessioned(sp => (AuthManager)sp.GetRequiredService<AuthenticationStateProvider>());
```

This registers `AuthManager` both as `AuthenticationStateProvider` (for Blazor) and as itself (for direct access).

## Advanced Topics

### Service Discovery and Resilience (Server-Side)

The server uses .NET Aspire's service defaults for:
- **Service discovery**: Automatically finding dependent services
- **Health checks**: Monitoring service health
- **OpenTelemetry**: Distributed tracing and metrics
- **Resilience**: Retry policies and circuit breakers

This is configured in [`WebApplicationBuilderExtensions.cs`](../src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs):

```csharp
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
    // http.AddStandardResilienceHandler(); // Uncomment for automatic retries
});
```

### Output Caching with Custom Policy

The project includes a sophisticated response caching system:

```csharp
services.AddOutputCache(options =>
{
    options.AddPolicy("AppResponseCachePolicy", policy =>
    {
        var builder = policy.AddPolicy<AppResponseCachePolicy>();
    }, excludeDefaultPolicy: true);
});
```

See the [Response Caching documentation](.docs/14-ResponseCaching.md) for more details.

### Database Context Registration

Entity Framework Core is registered with both pooling and factory patterns:

```csharp
services.AddPooledDbContextFactory<AppDbContext>(AddDbContext);
services.AddDbContextPool<AppDbContext>(AddDbContext);
```

This provides optimal performance for both direct usage and API controllers.

## Summary

The dependency injection architecture in this project:

✅ **Separates concerns** by layer and platform

✅ **Prevents duplicate registrations** through clear hierarchies

✅ **Optimizes service lifetimes** with `AddSessioned` for cross-platform code

✅ **Simplifies injection** with the `[AutoInject]` attribute

✅ **Enables platform-specific implementations** while maintaining a common interface

✅ **Follows ASP.NET Core best practices** for service registration

## Key Takeaways

1. **Always register services in the appropriate layer** - don't register in multiple places
2. **Use `AddSessioned` for user-specific state** in cross-platform client code
3. **Leverage `[AutoInject]` to reduce boilerplate** and automatically inherit base class dependencies
4. **Use interfaces for platform-specific implementations** to maintain consistency
5. **Follow the existing patterns** in the project for new services

---
