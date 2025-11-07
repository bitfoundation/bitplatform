# Stage 6: Exception Handling and Error Management

Welcome to Stage 6 of the Boilerplate project getting started guide. In this stage, you'll learn about the comprehensive exception handling system built into the project, which ensures robust error management across all layers of your application.

---

## 📋 Table of Contents

1. [Known vs Unknown Exceptions](#1-known-vs-unknown-exceptions)
2. [Safe Exception Throwing](#2-safe-exception-throwing)
3. [Exception Data with WithData()](#3-exception-data-with-withdata)
4. [Sending Additional Data to Client with WithExtensionData()](#4-sending-additional-data-to-client-with-withextensiondata)
5. [Automatic Exception Handling in Blazor](#5-automatic-exception-handling-in-blazor)
6. [Error Display UI](#6-error-display-ui)
7. [Exception Handlers in the Project](#7-exception-handlers-in-the-project)
8. [Practical Examples from the Codebase](#8-practical-examples-from-the-codebase)

---

## 1. Known vs Unknown Exceptions

The project distinguishes between two types of exceptions:

### Known Exceptions

**Location**: [`src/Shared/Exceptions/`](../src/Shared/Exceptions/)

Known exceptions inherit from the [`KnownException`](../src/Shared/Exceptions/KnownException.cs) base class. These are business logic exceptions with **user-friendly messages** that are **always displayed to end users** in all environments (Development, Staging, Production).

**Key Characteristics**:
- Their messages are often localized using resource files
- They represent expected error conditions (e.g., resource not found, validation errors)
- Logged at `LogLevel.Error`
- Safe to show to users in production

**Examples from the project**:

| Exception Class | File | Purpose |
|----------------|------|---------|
| `ResourceNotFoundException` | [`ResourceNotFoundException.cs`](../src/Shared/Exceptions/ResourceNotFoundException.cs) | Thrown when a requested resource doesn't exist (HTTP 404) |
| `BadRequestException` | [`BadRequestException.cs`](../src/Shared/Exceptions/BadRequestException.cs) | Thrown for invalid requests (HTTP 400) |
| `UnauthorizedException` | [`UnauthorizedException.cs`](../src/Shared/Exceptions/UnauthorizedException.cs) | Thrown when authentication is required (HTTP 401) |
| `ForbiddenException` | [`ForbiddenException.cs`](../src/Shared/Exceptions/ForbiddenException.cs) | Thrown when user lacks permissions (HTTP 403) |
| `DomainLogicException` | [`DomainLogicException.cs`](../src/Shared/Exceptions/DomainLogicException.cs) | Thrown for business rule violations |

**Code Example from [`KnownException.cs`](../src/Shared/Exceptions/KnownException.cs)**:

```csharp
public abstract partial class KnownException : Exception
{
    public KnownException(string message) : base(message)
    {
        Key = message;
    }

    public KnownException(LocalizedString message) : base(message.Value)
    {
        Key = message.Name;
    }

    public string? Key { get; set; }
}
```

### Unknown Exceptions

**Characteristics**:
- All other exceptions (e.g., `NullReferenceException`, `InvalidOperationException`, etc.)
- **Development environment**: The actual exception message and stack trace are shown to help developers debug
- **Production/Staging environments**: A generic "Unknown error" message is displayed to users for security reasons (to avoid exposing sensitive system information)
- Logged at `LogLevel.Critical`
- Still **logged with full details** for developers to investigate

**Security Benefit**: This prevents sensitive information (like database connection strings, file paths, or internal system details) from being exposed to end users in production.

---

## 2. Safe Exception Throwing

### 🎯 Important: Throwing Exceptions Does NOT Crash the Application

In this project, you can confidently throw exceptions **without worrying about application crashes**. All exceptions are automatically caught and handled by the framework.

**Why This Matters**:
- Developers can use exceptions for flow control without fear
- No need for excessive try-catch blocks in most cases
- The application remains stable even when unexpected errors occur
- Users always see appropriate error messages instead of crash screens

**Example from [`AttachmentController.cs`](../src/Server/Boilerplate.Server.Api/Controllers/AttachmentController.cs)**:

```csharp
[HttpGet("{attachmentId:guid}")]
public async Task<Stream> Get(Guid attachmentId, CancellationToken cancellationToken)
{
    var attachment = await DbContext.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
    
    if (attachment is null)
        throw new ResourceNotFoundException(); // Application continues working!
    
    var filePath = Path.Combine(uploadsDir, $"{attachment.FileName}{attachment.FileExtension}");
    
    if (System.IO.File.Exists(filePath) is false)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ImageCouldNotBeFound)]);
    
    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
}
```

---

## 3. Exception Data with WithData()

### Adding Context to Exceptions for Better Logging

The [`WithData()`](../src/Shared/Extensions/ExceptionExtensions.cs) extension method allows you to attach additional diagnostic data to exceptions. This data is **logged but NOT shown to end users**.

**Purpose**:
- Add contextual information for debugging
- Track additional variables that might help diagnose issues
- Enhance logging without exposing sensitive data to users

**Location**: [`src/Shared/Extensions/ExceptionExtensions.cs`](../src/Shared/Extensions/ExceptionExtensions.cs)

```csharp
public static class ExceptionExtensions
{
    /// <summary>
    /// Any custom properties specified here will be recorded along with general telemetry data, 
    /// including the client's IP address.
    /// </summary>
    public static TException WithData<TException>(this TException exception, string key, object? value)
        where TException : Exception
    {
        exception.Data[key] = value;
        return exception;
    }
}
```

**Example Usage**:

```csharp
// Example 1: Adding single data value
if (product == null)
{
    throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductNotFound)])
        .WithData("ProductId", productId)
        .WithData("UserId", User.GetUserId());
}

// Example 2: Adding multiple data values
throw new InvalidOperationException("Payment processing failed")
    .WithData("OrderId", order.Id)
    .WithData("Amount", order.TotalAmount)
    .WithData("PaymentProvider", paymentProvider);
```

**What Gets Logged**:
All the `WithData()` values are automatically included in the log entry along with:
- Exception type and message
- Stack trace
- Client IP address (server-side)
- Request ID
- User ID (if authenticated)
- Timestamp

---

## 4. Sending Additional Data to Client with WithExtensionData()

### RFC 7807 Problem Details

The project sends **RFC 7807-compliant** error responses to clients. This is a standardized format for HTTP API error responses.

**Location**: [`src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs`](../src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs)

### Server-Side: Adding Extension Data

```csharp
public static class KnownExceptionExtensions
{
    /// <summary>
    /// Custom properties specified here will be included in the client's response via AppProblemDetails.Extensions  
    /// and logged alongside general telemetry data, including the client's IP address etc.
    /// </summary>
    public static TException WithExtensionData<TException>(this TException exception, string key, object? value)
        where TException : KnownException
    {
        exception.Data["__AppProblemDetailsExtensionsData"] ??= new Dictionary<string, object?>();
        
        var appProblemExtensionsData = (Dictionary<string, object?>)exception.Data["__AppProblemDetailsExtensionsData"]!;
        appProblemExtensionsData[key] = value;
        
        return exception;
    }
}
```

**Example: Sending Data to Client**:

```csharp
// Server-side controller
if (subscription.IsExpired)
{
    throw new BadRequestException("Your subscription has expired")
        .WithExtensionData("ExpirationDate", subscription.ExpiresAt)
        .WithExtensionData("RenewalUrl", "/subscription/renew");
}
```

### Client-Side: Reading Extension Data

**Location**: [`KnownException.cs`](../src/Shared/Exceptions/KnownException.cs) (shared between server and client)

```csharp
public abstract partial class KnownException : Exception
{
    /// <summary>
    /// Read KnownExceptionExtensions.WithExtensionData comments.
    /// </summary>
    public bool TryGetExtensionDataValue<T>(string key, out T value)
    {
        value = default!;

        if (Data[key] is object valueObj)
        {
            if (valueObj is T)
            {
                value = (T)valueObj;
            }
            else if (valueObj is JsonElement jsonElement)
            {
                value = jsonElement.Deserialize(AppJsonContext.Default.Options.GetTypeInfo<T>())!;
            }
            return true;
        }

        return false;
    }
}
```

**Example: Client-Side Usage**:

```csharp
try
{
    await ProductController.UpdateProduct(productDto);
}
catch (BadRequestException ex)
{
    if (ex.TryGetExtensionDataValue<DateTime>("ExpirationDate", out var expirationDate))
    {
        SnackBarService.Error($"Subscription expired on {expirationDate:d}");
    }
    
    if (ex.TryGetExtensionDataValue<string>("RenewalUrl", out var renewalUrl))
    {
        NavigationManager.NavigateTo(renewalUrl);
    }
}
```

**Key Difference**:
- `WithData()`: For internal logging only (NOT sent to client)
- `WithExtensionData()`: Sent to client AND logged

---

## 5. Automatic Exception Handling in Blazor

### Enhanced Component Lifecycle Methods

The project provides safer alternatives to Blazor's standard lifecycle methods that automatically handle exceptions.

**Location**: [`src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](../src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)

### Enhanced Lifecycle Methods

| Standard Blazor Method | Enhanced Method in AppComponentBase | Execution |
|------------------------|-------------------------------------|-----------|
| `OnInitializedAsync()` | `OnInitAsync()` | Once when component initializes |
| `OnParametersSetAsync()` | `OnParamsSetAsync()` | When parameters are set/updated |
| `OnAfterRenderAsync()` (first render) | `OnAfterFirstRenderAsync()` | Only on first render |

**How It Works** (from [`AppComponentBase.cs`](../src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)):

```csharp
protected sealed override async Task OnInitializedAsync()
{
    try
    {
        await base.OnInitializedAsync();
        await OnInitAsync(); // Your code here
    }
    catch (Exception exp)
    {
        HandleException(exp); // Automatically handled
    }
}

/// <summary>
/// A safer alternative to OnInitializedAsync that catches and handles all exceptions internally
/// </summary>
protected virtual Task OnInitAsync()
{
    return Task.CompletedTask;
}
```

**Example Usage in Your Components**:

```csharp
public partial class ProductListPage : AppPageBase
{
    [AutoInject] private IProductController productController = default!;
    
    private List<ProductDto> products = [];

    protected override async Task OnInitAsync()
    {
        // Any exception here is automatically caught and handled
        products = await productController.GetProducts(CurrentCancellationToken);
    }
}
```

### WrapHandled() for Event Handlers

While unhandled exceptions in event handlers are also caught automatically, using `WrapHandled()` provides **more consistent error handling** and better control over error display.

**From [`AppComponentBase.cs`](../src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)**:

```csharp
/// <summary>
/// Executes passed action that catches and handles all exceptions internally
/// </summary>
public virtual Action WrapHandled(Action action, ...)
{
    return () =>
    {
        try
        {
            action();
        }
        catch (Exception exp)
        {
            HandleException(exp, null, lineNumber, memberName, filePath);
        }
    };
}

public virtual Func<Task> WrapHandled(Func<Task> func, ...)
{
    return async () =>
    {
        try
        {
            await func();
        }
        catch (Exception exp)
        {
            HandleException(exp, null, lineNumber, memberName, filePath);
        }
    };
}
```

**Razor File Examples**:

```xml
<!-- ✅ RECOMMENDED: Using WrapHandled -->
<BitButton OnClick="WrapHandled(DeleteItem)">Delete</BitButton>
<BitButton OnClick="WrapHandled(async () => await SaveChanges())">Save</BitButton>

<!-- ⚠️ Also works but less consistent -->
<BitButton OnClick="DeleteItem">Delete</BitButton>
```

**Code-Behind Example**:

```csharp
public partial class ProductEditPage : AppPageBase
{
    private async Task SaveProduct()
    {
        // Any exception here is automatically handled by WrapHandled
        await productController.UpdateProduct(product, CurrentCancellationToken);
        SnackBarService.Success("Product saved successfully!");
        NavigationManager.NavigateTo(PageUrls.Products);
    }
}
```

---

## 6. Error Display UI

When exceptions occur, they are displayed to users through a sophisticated UI system:

### Exception Display Types

**From [`IExceptionHandler.cs`](../src/Client/Boilerplate.Client.Core/Services/Contracts/IExceptionHandler.cs)**:

```csharp
public enum ExceptionDisplayKind
{
    /// <summary>
    /// No error message is shown to the user.
    /// </summary>
    None,
    
    /// <summary>
    /// Requires the user to acknowledge the error (e.g., by tapping "OK").
    /// </summary>
    Interrupting,
    
    /// <summary>
    /// Shows an auto-dismissed message (e.g., a snack bar)
    /// </summary>
    NonInterrupting,
    
    /// <summary>
    /// Automatically selects the exception display type based on the exception type.
    /// </summary>
    Default
}
```

### How Display Type is Determined

**From [`ClientExceptionHandlerBase.cs`](../src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs)**:

```csharp
private ExceptionDisplayKind GetDisplayKind(Exception exception)
{
    if (exception is ServerConnectionException)
        return ExceptionDisplayKind.NonInterrupting; // Snack bar
    
    if (exception is UnauthorizedException)
        return ExceptionDisplayKind.NonInterrupting; // Snack bar
    
    return ExceptionDisplayKind.Interrupting; // Modal dialog
}
```

### UI Components

1. **Interrupting (Modal Dialog)**:
   - Uses `BitMessageBoxService`
   - Requires user to click "OK"
   - Used for critical errors that need acknowledgment

2. **NonInterrupting (Snack Bar)**:
   - Uses `SnackBarService`
   - Auto-dismisses after a few seconds
   - Used for less critical errors (connection issues, authorization)

3. **Error Boundary**:
   - Catches unhandled exceptions in component trees
   - Provides fallback UI
   - Located in [`MainLayout.razor`](../src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor)

---

## 7. Exception Handlers in the Project

The project has multiple exception handlers for different platforms and contexts:

### Exception Handler Hierarchy

```
SharedExceptionHandler (Base class in Shared project)
    ├── ServerExceptionHandler (Server.Api)
    │   └── Implements IProblemDetailsWriter
    │   └── Generates RFC 7807 responses
    │
    └── ClientExceptionHandlerBase (Client.Core)
        ├── WebServerExceptionHandler (Server.Web for Blazor Server & SSR)
        ├── WebClientExceptionHandler (Client.Web for WebAssembly)
        ├── MauiExceptionHandler (Client.Maui)
        └── WindowsExceptionHandler (Client.Windows)
```

### 1. SharedExceptionHandler

**Location**: [`src/Shared/Services/SharedExceptionHandler.cs`](../src/Shared/Services/SharedExceptionHandler.cs)

**Purpose**: Base class providing common exception handling logic

**Key Methods**:
- `GetExceptionMessageToShow()`: Determines what message to show users
- `GetExceptionMessageToLog()`: Determines what to log
- `UnWrapException()`: Unwraps aggregate/inner exceptions
- `IgnoreException()`: Decides which exceptions to ignore (e.g., `TaskCanceledException`)

### 2. ServerExceptionHandler

**Location**: [`src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs`](../src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs)

**Purpose**: Handles API exceptions and generates RFC 7807 problem details

**Key Features**:
- Implements `IProblemDetailsWriter` for ASP.NET Core
- Logs exceptions with full context (IP, user ID, request ID)
- Generates `AppProblemDetails` responses
- Distinguishes between KnownException (LogLevel.Error) and others (LogLevel.Critical)

**Code Example**:

```csharp
public partial class ServerExceptionHandler : SharedExceptionHandler, IProblemDetailsWriter
{
    private void Handle(Exception exception,
        Dictionary<string, object?>? parameters,
        HttpContext? httpContext,
        out int statusCode,
        out AppProblemDetails? problemDetails)
    {
        var knownException = exception as KnownException;
        statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);
        
        // The details of all exceptions are returned only in dev mode
        // In production, only KnownException details are returned
        var message = GetExceptionMessageToShow(exception);
        
        if (IgnoreException(exception) is false)
        {
            using var scope = logger.BeginScope(data);
            
            if (exception is KnownException)
            {
                logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                logger.LogCritical(exception, exceptionMessageToLog);
            }
        }
        
        problemDetails = new AppProblemDetails
        {
            Title = message,
            Status = statusCode,
            Type = knownException?.GetType().FullName ?? typeof(UnknownException).FullName,
            Instance = instance,
            Extensions = new Dictionary<string, object?>() // Extension data goes here
        };
    }
}
```

### 3. ClientExceptionHandlerBase

**Location**: [`src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs`](../src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs)

**Purpose**: Base handler for all client-side platforms

**Key Features**:
- Determines display kind (Interrupting vs NonInterrupting)
- Shows errors via SnackBarService or MessageBoxService
- Includes telemetry context (platform, version, user ID, etc.)
- Generates unique exception IDs

**Code Example**:

```csharp
public abstract partial class ClientExceptionHandlerBase : SharedExceptionHandler, IExceptionHandler
{
    protected virtual void Handle(Exception exception,
        ExceptionDisplayKind displayKind,
        Dictionary<string, object> parameters)
    {
        using (var scope = Logger.BeginScope(parameters))
        {
            if (exception is KnownException)
            {
                Logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                Logger.LogCritical(exception, exceptionMessageToLog);
            }
        }

        if (displayKind is ExceptionDisplayKind.NonInterrupting)
        {
            SnackBarService.Error("Boilerplate", exceptionMessageToShow);
        }
        else if (displayKind is ExceptionDisplayKind.Interrupting)
        {
            _ = MessageBoxService.Show(Localizer[nameof(AppStrings.Error)], exceptionMessageToShow);
        }
    }
}
```

### 4. WebServerExceptionHandler

**Location**: [`src/Server/Boilerplate.Server.Web/Services/WebServerExceptionHandler.cs`](../src/Server/Boilerplate.Server.Web/Services/WebServerExceptionHandler.cs)

**Purpose**: Handles exceptions in **Blazor Server** and during **pre-rendering**

**Key Features**:
- Inherits from `ClientExceptionHandlerBase` (because Blazor Server runs on server but displays UI like client)
- Sets HTTP response status codes when possible
- Prevents caching of error responses

```csharp
public partial class WebServerExceptionHandler : ClientExceptionHandlerBase
{
    [AutoInject] IHttpContextAccessor httpContextAccessor = default!;

    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        if (httpContextAccessor.HttpContext is not null && httpContextAccessor.HttpContext.Response.HasStarted is false)
        {
            // Set status code for non-streaming pre-rendering to prevent error caching
            var statusCode = (int)(exception is RestException restExp ? restExp.StatusCode : HttpStatusCode.InternalServerError);
            httpContextAccessor.HttpContext.Response.StatusCode = statusCode;
        }

        base.Handle(exception, displayKind, parameters);
    }
}
```

### 5. WebClientExceptionHandler

**Location**: [`src/Client/Boilerplate.Client.Web/Services/WebClientExceptionHandler.cs`](../src/Client/Boilerplate.Client.Web/Services/WebClientExceptionHandler.cs)

**Purpose**: Handles exceptions in **Blazor WebAssembly**

```csharp
public partial class WebClientExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, displayKind, parameters);
    }
}
```

### 6. MauiExceptionHandler

**Location**: [`src/Client/Boilerplate.Client.Maui/Services/MauiExceptionHandler.cs`](../src/Client/Boilerplate.Client.Maui/Services/MauiExceptionHandler.cs)

**Purpose**: Handles exceptions in **.NET MAUI** (Android, iOS, macOS, Windows via MAUI)

**Features**:
- Platform-specific error handling
- Integration point for Firebase Crashylicst or other mobile analytics

```csharp
public partial class MauiExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        // Optional: Integrate Firebase Crashlytics here. This keeps the package installation limited to .NET MAUI projects,
        // ensuring it's not downloaded for unsupported platforms like Windows or Web where it provides no functionality.

        base.Handle(exception, displayKind, parameters);
    }
}
```

### 7. WindowsExceptionHandler

**Location**: [`src/Client/Boilerplate.Client.Windows/Services/WindowsExceptionHandler.cs`](../src/Client/Boilerplate.Client.Windows/Services/WindowsExceptionHandler.cs)

**Purpose**: Handles exceptions in **Windows Forms Blazor Hybrid**

```csharp
public partial class WindowsExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, displayKind, parameters);
    }
}
```

### Exception Handler Flow

```
Exception Occurs
     ↓
Where does it occur?
     ↓
     ├─→ API Controller → ServerExceptionHandler
     │                       ↓
     │                    Generate RFC 7807 Response
     │                       ↓
     │                    Log to Server Logs
     │
     ├─→ Blazor Server/SSR → WebServerExceptionHandler
     │                          ↓
     │                       Display UI Error
     │                          ↓
     │                       Log to Client Logs
     │
     ├─→ Blazor WASM → WebClientExceptionHandler
     │                    ↓
     │                 Display UI Error
     │                    ↓
     │                 Log to Client Logs
     │
     ├─→ MAUI App → MauiExceptionHandler
     │                 ↓
     │              Display UI Error
     │                 ↓
     │              Log to Client Logs
     │
     └─→ Windows App → WindowsExceptionHandler
                          ↓
                       Display UI Error
                          ↓
                       Log to Client Logs
                          
All Logs → Sentry/App Insights/OpenTelemetry
```

---

## 8. Practical Examples from the Codebase

### Example 1: Simple Resource Not Found

**From [`AttachmentController.cs`](../src/Server/Boilerplate.Server.Api/Controllers/AttachmentController.cs)**:

```csharp
[HttpGet("{attachmentId:guid}")]
public async Task<Stream> Get(Guid attachmentId, CancellationToken cancellationToken)
{
    var attachment = await DbContext.Attachments
        .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
    
    if (attachment is null)
        throw new ResourceNotFoundException();
    
    var filePath = Path.Combine(uploadsDir, $"{attachment.FileName}{attachment.FileExtension}");
    
    if (System.IO.File.Exists(filePath) is false)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ImageCouldNotBeFound)]);
    
    return new FileStream(filePath, FileMode.Open, FileAccess.Read);
}
```

### Example 2: Exception with Extension Data

**Hypothetical example** (you can add this pattern to your controllers):

```csharp
[HttpDelete("{productId:guid}")]
public async Task DeleteProduct(Guid productId, CancellationToken cancellationToken)
{
    var product = await DbContext.Products.FindAsync([productId], cancellationToken);
    
    if (product is null)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ProductNotFound)])
            .WithData("ProductId", productId)
            .WithExtensionData("SuggestedAction", "browse_catalog");
    
    // Check if product is referenced by orders
    var hasOrders = await DbContext.Orders.AnyAsync(o => o.ProductId == productId, cancellationToken);
    
    if (hasOrders)
    {
        throw new BadRequestException("Cannot delete product with existing orders")
            .WithData("ProductId", productId)
            .WithData("ProductName", product.Name)
            .WithExtensionData("OrderCount", await DbContext.Orders.CountAsync(o => o.ProductId == productId))
            .WithExtensionData("SuggestedAction", "archive_instead");
    }
    
    DbContext.Products.Remove(product);
    await DbContext.SaveChangesAsync(cancellationToken);
}
```

**Client-side handling**:

```csharp
try
{
    await productController.DeleteProduct(productId);
    SnackBarService.Success("Product deleted successfully");
}
catch (BadRequestException ex) when (ex.Message.Contains("existing orders"))
{
    if (ex.TryGetExtensionDataValue<int>("OrderCount", out var orderCount))
    {
        await MessageBoxService.Show(
            "Cannot Delete", 
            $"This product has {orderCount} existing orders. Consider archiving instead.");
    }
    
    if (ex.TryGetExtensionDataValue<string>("SuggestedAction", out var action) && action == "archive_instead")
    {
        // Show archive option
    }
}
```

### Example 3: Component Exception Handling

**Page Component with Automatic Exception Handling**:

```csharp
public partial class ProductListPage : AppPageBase
{
    [AutoInject] private IProductController productController = default!;
    
    private List<ProductDto> products = [];
    private bool isLoading;

    // Exceptions in OnInitAsync are automatically handled
    protected override async Task OnInitAsync()
    {
        isLoading = true;
        
        // If this throws, user sees error message automatically
        products = await productController.GetProducts(CurrentCancellationToken);
        
        isLoading = false;
    }

    // Event handler with WrapHandled
    private async Task DeleteProduct(Guid productId)
    {
        var confirmed = await MessageBoxService.Show("Confirm", "Delete this product?", BitMessageBoxType.YesNo);
        
        if (confirmed == BitMessageBoxResult.Yes)
        {
            // Any exception here is handled by WrapHandled
            await productController.DeleteProduct(productId, CurrentCancellationToken);
            products = products.Where(p => p.Id != productId).ToList();
            SnackBarService.Success("Product deleted");
        }
    }
}
```

**Razor file**:

```xml
@if (isLoading)
{
    <LoadingComponent />
}
else
{
    <BitDataGrid Items="products">
        <BitDataGridPropertyColumn Property="@(p => p.Name)" />
        <BitDataGridPropertyColumn Property="@(p => p.Price)" />
        <BitDataGridTemplateColumn>
            <Template Context="product">
                <BitButton OnClick="WrapHandled(async () => await DeleteProduct(product.Id))">
                    Delete
                </BitButton>
            </Template>
        </BitDataGridTemplateColumn>
    </BitDataGrid>
}
```

---

## 🎯 Key Takeaways

1. **Safe to Throw**: Throwing exceptions won't crash your app
2. **Two Types**: KnownException (user-friendly) vs Unknown (developer-focused)
3. **Context Matters**: Use `WithData()` for logs, `WithExtensionData()` for client responses
4. **Automatic Handling**: Use `OnInitAsync()`, `OnParamsSetAsync()`, `OnAfterFirstRenderAsync()`
5. **Wrap Handlers**: Use `WrapHandled()` for event handlers
6. **Platform-Specific**: Different handlers for API, Web, MAUI, Windows
7. **User Experience**: Interrupting (modal) vs NonInterrupting (snack bar) errors
8. **RFC 7807**: Standard error responses for APIs

---

## 📚 Related Files to Explore

- [`src/Shared/Exceptions/`](../src/Shared/Exceptions/) - All exception classes
- [`src/Shared/Extensions/ExceptionExtensions.cs`](../src/Shared/Extensions/ExceptionExtensions.cs) - WithData extension
- [`src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs`](../src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs) - WithExtensionData
- [`src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](../src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs) - Lifecycle methods
- [`src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs`](../src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs) - API handler
- [`src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs`](../src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs) - Client base handler

---
