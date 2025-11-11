# Stage 6: Exception Handling and Error Management

Welcome to Stage 6! In this stage, you will learn about the comprehensive exception handling and error management system built into the Boilerplate project.

---

## Overview

The project implements a robust exception handling architecture that:
- **Does NOT crash the application** when exceptions occur
- **Automatically handles exceptions** in Blazor components and pages
- **Displays user-friendly error messages** to end users
- **Logs detailed diagnostic information** for developers
- **Differentiates between known and unknown exceptions**
- **Provides multi-platform exception handlers** for Server, Web, MAUI, and Windows

---

## Known vs Unknown Exceptions

The project distinguishes between two types of exceptions:

### Known Exceptions

**Known Exceptions** are exceptions that inherit from the `KnownException` base class, located in [`src/Shared/Exceptions/KnownException.cs`](/src/Shared/Exceptions/KnownException.cs).

These are **expected, business-logic exceptions** that represent predictable error scenarios.

**Characteristics:**
- Their messages are **displayed directly to users** (user-friendly)
- They are **localized** using `IStringLocalizer<AppStrings>`
- They are logged as **warnings/errors** (not critical)
- They do NOT indicate bugs in the code

**Examples of Known Exceptions in the project:**

1. **`DomainLogicException`** ([`src/Shared/Exceptions/DomainLogicException.cs`](/src/Shared/Exceptions/DomainLogicException.cs))
   - Used for business rule violations
   - Example: "Cannot delete a category that has products"

2. **`ResourceNotFoundException`** ([`src/Shared/Exceptions/ResourceNotFoundException.cs`](/src/Shared/Exceptions/ResourceNotFoundException.cs))
   - Thrown when a requested resource doesn't exist
   - HTTP Status Code: 404 (Not Found)

3. **`BadRequestException`** ([`src/Shared/Exceptions/BadRequestException.cs`](/src/Shared/Exceptions/BadRequestException.cs))
   - Used for invalid client requests
   - HTTP Status Code: 400 (Bad Request)

4. **`UnauthorizedException`** ([`src/Shared/Exceptions/UnauthorizedException.cs`](/src/Shared/Exceptions/UnauthorizedException.cs))
   - Thrown when authentication is required
   - HTTP Status Code: 401 (Unauthorized)

5. **`ServerConnectionException`** ([`src/Shared/Exceptions/ServerConnectionException.cs`](/src/Shared/Exceptions/ServerConnectionException.cs))
   - Indicates connectivity issues between client and server

**All Known Exceptions inherit from `RestException`** ([`src/Shared/Exceptions/RestException.cs`](/src/Shared/Exceptions/RestException.cs)), which inherits from `KnownException` and provides HTTP status code mapping for REST APIs.

---

### Unknown Exceptions

**Unknown Exceptions** are all other exceptions (e.g., `NullReferenceException`, `InvalidOperationException`, `ArgumentException`, etc.).

**Characteristics:**
- They represent **bugs or unexpected errors** in the code
- Their detailed messages are **NOT shown to end users** (for security)
- In **Production**, users see a generic message: *"An unknown error occurred"*
- In **Development**, users see the **full exception details** (stack trace, etc.)
- They are logged as **critical errors**

**Example:**
```csharp
// This throws a NullReferenceException (Unknown Exception)
string? name = null;
int length = name.Length; // Bug in the code!
```

In Production, the user would see:
> "An unknown error occurred"

In Development, the user would see:
> "System.NullReferenceException: Object reference not set to an instance of an object. at MyComponent.OnInitAsync()..."

**This behavior is controlled by the `SharedExceptionHandler`:**

```csharp
// From src/Shared/Services/SharedExceptionHandler.cs
protected string GetExceptionMessageToShow(Exception exception)
{
    if (exception is KnownException)
        return exception.Message; // Show actual message for Known Exceptions

    if (AppEnvironment.IsDevelopment())
        return exception.ToString(); // Show full details in Development

    return Localizer[nameof(AppStrings.UnknownException)]; // Generic message in Production
}
```

---

## Safe Exception Throwing

### Important: Throwing Exceptions Does NOT Crash the Application

In this project, **throwing exceptions is safe** and does **NOT crash the application**. The exception handling system automatically catches and handles exceptions at multiple levels.

**Example:**
```csharp
protected override async Task OnInitAsync()
{
    var user = await UserController.GetUserById(userId, CurrentCancellationToken);
    
    if (user == null)
    {
        // This does NOT crash the app!
        throw new ResourceNotFoundException("User not found");
    }
    
    // Continue processing...
}
```

The exception is automatically:
1. **Caught** by `AppComponentBase`'s enhanced lifecycle methods
2. **Logged** with full context and telemetry
3. **Displayed** to the user via a message box or snack bar
4. The component **remains functional** (no crash)

---

## Exception Data with WithData()

The `WithData()` extension method allows developers to attach **additional contextual information** to exceptions for better logging and debugging.

**Location:** [`src/Shared/Extensions/ExceptionExtensions.cs`](/src/Shared/Extensions/ExceptionExtensions.cs)

### Syntax

```csharp
// Single key-value pair
exception.WithData("key", value)

// Multiple key-value pairs
exception.WithData(new()
{
    { "UserId", userId },
    { "ProductId", productId },
    { "Timestamp", DateTimeOffset.UtcNow }
})
```

### Real Example from the Project

From [`src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.EmailConfirmation.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.EmailConfirmation.cs):

```csharp
var user = await UserManager.FindByEmailAsync(request.Email, cancellationToken)
    ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)])
        .WithData("Email", request.Email);

if (user.EmailConfirmed)
{
    throw new BadRequestException(Localizer[nameof(AppStrings.EmailAlreadyConfirmed)])
        .WithData("UserId", user.Id);
}
```

### Benefits

1. **Enhanced Logging:** The data is automatically included in log entries
2. **Telemetry:** Works seamlessly with Application Insights, Sentry, etc.
3. **Debugging:** Helps diagnose issues in production without exposing sensitive data to users
4. **Traceability:** Connect exceptions to specific entities (User, Product, Order, etc.)

**Note:** Data added with `WithData()` on the server is **NOT sent to the client** - it's for logging only. Use `WithExtensionData()` (see below) to send data to the client.

---

## Sending Additional Data to the Client using WithExtensionData()

The `WithExtensionData()` extension method allows you to send **additional contextual data to the client** along with the error response. This is useful when the client needs specific information to handle the error appropriately.

**Location:** [`src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs`](/src/Server/Boilerplate.Server.Api/Extensions/KnownExceptionExtensions.cs)

### Important: Only Works with Known Exceptions

**CRITICAL:** `WithExtensionData()` **ONLY** works with exceptions that inherit from `KnownException`.

### RFC 7807 Compliance

The project sends **RFC 7807-compliant** error responses to clients. This is a standardized format for HTTP API error responses:

```json
{
  "type": "Boilerplate.Shared.Exceptions.TooManyRequestsException",
  "title": "Please wait 1 minute before requesting another reset password token",
  "status": 429,
  "instance": "POST /api/identity/SendResetPasswordToken",
  "extensions": {
    "key": "WaitForResetPasswordTokenRequestResendDelay",
    "traceId": "00-abc123...",
    "TryAgainIn": "00:01:00"
  }
}
```

### Syntax

```csharp
// Single key-value pair
exception.WithExtensionData("key", value)

// Multiple key-value pairs
exception.WithExtensionData(new Dictionary<string, object?>
{
    { "TryAgainIn", TimeSpan.FromMinutes(1) },
    { "RetryAfter", DateTimeOffset.UtcNow.AddMinutes(1) }
})
```

### Real Example from the Project

From [`src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/IdentityController.cs):

```csharp
if (signInResult.IsLockedOut)
{
    var tryAgainIn = (user.LockoutEnd! - DateTimeOffset.UtcNow).Value;
    
    throw new BadRequestException(
        Localizer[nameof(AppStrings.UserLockedOut), 
        tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)])
        .WithData("UserId", user.Id)  // For server logging only
        .WithExtensionData("TryAgainIn", tryAgainIn);  // Sent to client
}
```

In these examples:
- `WithData("UserId", user.Id)`: Logged on the server, **NOT sent to client**
- `WithExtensionData("TryAgainIn", tryAgainIn)`: **Sent to client** in the error response

### Reading Extension Data on the Client

The client can read the extension data using `TryGetExtensionDataValue<T>()`:

From [`src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor.cs):

```csharp
catch (KnownException e)
{
    // Read the TryAgainIn value sent from the server
    if (e.TryGetExtensionDataValue<TimeSpan>("TryAgainIn", out var tryAgainIn))
    {
        // Disable the sign-in button until the lockout period expires
        // Show countdown timer, etc.
    }
}
```

### Use Cases

**Common scenarios where you'd use `WithExtensionData()`:**

1. **Rate Limiting:** Tell the client how long to wait before retrying
   ```csharp
   throw new TooManyRequestsException("Too many requests")
       .WithExtensionData("RetryAfter", TimeSpan.FromMinutes(5));
   ```

2. **Account Lockouts:** Provide lockout duration
   ```csharp
   throw new BadRequestException("Account locked")
       .WithExtensionData("TryAgainIn", lockoutDuration);
   ```

3. **Business Logic Errors:** Provide actionable information
   ```csharp
   throw new ConflictException("Product out of stock")
       .WithExtensionData("AvailableQuantity", 0)
       .WithExtensionData("NextRestockDate", restockDate);
   ```

---

## Automatic Exception Handling in Blazor

The project provides **automatic exception handling** in Blazor components and pages through enhanced lifecycle methods.

### Enhanced Lifecycle Methods

When your components inherit from `AppComponentBase` or your pages inherit from `AppPageBase`, you have access to enhanced lifecycle methods that automatically catch exceptions:

**Location:** [`src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)

#### Available Enhanced Methods

1. **`OnInitAsync()`** - Replacement for `OnInitializedAsync()`
2. **`OnParamsSetAsync()`** - Replacement for `OnParametersSetAsync()`
3. **`OnAfterFirstRenderAsync()`** - Enhanced version of `OnAfterRenderAsync(firstRender: true)`

**Example:**
```csharp
public partial class MyComponent : AppComponentBase
{
    protected override async Task OnInitAsync()
    {
        // Any exception thrown here is automatically caught and handled!
        var data = await ApiClient.GetData(CurrentCancellationToken);
        
        if (data == null)
        {
            throw new DomainLogicException("No data available");
            // This will NOT crash the app - it will show an error message to the user
        }
    }
    
    protected override async Task OnParamsSetAsync()
    {
        // Automatically catches exceptions
        await LoadUserProfile();
    }
    
    protected override async Task OnAfterFirstRenderAsync()
    {
        // Automatically catches exceptions
        await JSRuntime.InvokeVoidAsync("initializeChart");
    }
}
```

**What happens when an exception occurs?**
1. Exception is **caught automatically**
2. Exception is **logged** with full context (component name, file path, line number, etc.)
3. Exception is **handled** by the appropriate exception handler
4. User sees a **friendly error message** (message box or snack bar)
5. Component **remains functional** (no crash)

---

### Event Handlers in Razor: WrapHandled()

For event handlers in `.razor` files (button clicks, form submissions, etc.), you must use the `WrapHandled()` method to enable automatic exception handling.

**Syntax:**
```xml
<!-- Simple method call -->
<BitButton OnClick="WrapHandled(MyMethod)">Click Me</BitButton>

<!-- Lambda expression -->
<BitButton OnClick="WrapHandled(async () => await SaveData())">Save</BitButton>

<!-- With parameters -->
<BitButton OnClick="WrapHandled((MouseEventArgs e) => HandleClick(e))">Click</BitButton>
```

**Real Examples from the Project:**

From [`src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Identity/SignIn/SignInPanel.razor):

```xml
<EditForm Model="model" OnSubmit="WrapHandled(DoSignIn)" novalidate>
    <AppDataAnnotationsValidator @ref="validatorRef" />
    
    <!-- Form fields -->
    
    <BitButton ButtonType="BitButtonType.Submit" AutoLoading IsEnabled="isWaiting is false">
        @Localizer[nameof(AppStrings.SignIn)]
    </BitButton>
</EditForm>

<BitButton OnClick="WrapHandled(PasswordlessSignIn)" 
           IconName="@BitIconName.Fingerprint"
           IsEnabled="isWaiting is false">
    Passwordless Sign-In
</BitButton>
```

**Implementation of WrapHandled() in AppComponentBase:**

```xml
<EditForm Model="model" OnValidSubmit="WrapHandled(Submit)" novalidate>
    <!-- Password reset form -->
</EditForm>

<BitOtpInput OnFill="WrapHandled(HandleContinue)" />

<BitButton OnClick="WrapHandled(Resend)">
    @Localizer[nameof(AppStrings.Resend)]
</BitButton>
```

**Why is WrapHandled() necessary?**

Without `WrapHandled()`, unhandled exceptions in event handlers would:
1. Trigger the **Error Boundary** (whole UI section crashes)
2. Provide a **poor user experience**

With `WrapHandled()`:
1. Exception is **caught and logged** with full context (file path, line number, member name)
2. User sees a **friendly error message**
3. Component **remains functional**
4. No Error Boundary triggered

---

## Error Display UI

When exceptions occur, the project displays user-friendly error messages through two mechanisms:

### 1. Interrupting Errors (Message Box)

For **critical errors** that require user acknowledgment, a **message box** is displayed.

**When used:**
- Unknown exceptions (bugs)
- Critical business logic errors

**Example:**
```
┌─────────────────────────────────────┐
│ Error                               │
├─────────────────────────────────────┤
│ Failed to save the product.         │
│ Please try again.                   │
│                                     │
│              [OK]                   │
└─────────────────────────────────────┘
```

**Handled by:** `BitMessageBoxService` in [`ClientExceptionHandlerBase`](/src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs)

---

### 2. Non-Interrupting Errors (Snack Bar)

For **less critical errors** that don't require immediate action, a **snack bar** (toast notification) is displayed.

**Example:**
```
┌─────────────────────────────────────────┐
│ ⚠️ You are currently offline            │
└─────────────────────────────────────────┘
```

**Handled by:** `SnackBarService` in [`ClientExceptionHandlerBase`](/src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs)

---

### 3. Error Boundary (Last Resort)

If an exception is **NOT caught** by the enhanced lifecycle methods or `WrapHandled()`, the **Error Boundary** is triggered.

**Location:** [`src/Client/Boilerplate.Client.Core/Components/AppErrorBoundary.razor`](/src/Client/Boilerplate.Client.Core/Components/AppErrorBoundary.razor)

The Error Boundary displays:
- **Title:** "Something Went Wrong"
- **Exception details** (if in Development mode)
- **Actions:** Home, Refresh, Recover, Diagnostic

**Example:**
```
┌─────────────────────────────────────────┐
│ Something Went Wrong                    │
├─────────────────────────────────────────┤
│ An unexpected error occurred.           │
│                                         │
│ [Home] [Refresh] [Recover] [🔧]        │
└─────────────────────────────────────────┘
```

**When to use:**
- You should **avoid** triggering the Error Boundary
- Use `WrapHandled()` for event handlers
- Use enhanced lifecycle methods (`OnInitAsync()`, etc.)

---

## Exception Handlers in the Project

The project includes multiple exception handlers for different platforms, all inheriting from `SharedExceptionHandler`.

### 1. ServerExceptionHandler

**Location:** [`src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs`](/src/Server/Boilerplate.Server.Api/Services/ServerExceptionHandler.cs)

**Purpose:** Handles exceptions on the **server-side** (API controllers).

**Key Responsibilities:**
- Converts exceptions to **RFC 7807-compliant** `ProblemDetails` responses
- Attaches `Request-Id` header for log correlation
- Maps exceptions to **HTTP status codes** (404, 400, 500, etc.)
- Logs exceptions with full telemetry (Activity ID, User ID, Client IP, etc.)
- Differentiates between **Development** and **Production** environments

**Example of generated error response:**
```json
{
  "type": "Boilerplate.Shared.Exceptions.ResourceNotFoundException",
  "title": "User not found",
  "status": 404,
  "instance": "GET /api/user/123",
  "extensions": {
    "key": "UserNotFound",
    "traceId": "00-abc123...",
    "Email": "user@example.com"
  }
}
```

---

### 2. SharedExceptionHandler

**Location:** [`src/Shared/Services/SharedExceptionHandler.cs`](/src/Shared/Services/SharedExceptionHandler.cs)

**Purpose:** Provides **shared exception handling logic** used by both server and client.

**Key Responsibilities:**
- `GetExceptionMessageToShow()`: Determines what message to display to users
  - Known exceptions: Show the actual message
  - Unknown exceptions (Production): Show generic "Unknown error"
  - Unknown exceptions (Development): Show full stack trace
- `GetExceptionMessageToLog()`: Formats exception for logging (includes inner exceptions)
- `UnWrapException()`: Unwraps `AggregateException` and `TargetInvocationException`
- `IgnoreException()`: Determines if an exception should be logged
- `GetExceptionData()`: Extracts all data attached to the exception

---

### 3. ClientExceptionHandlerBase

**Location:** [`src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs`](/src/Client/Boilerplate.Client.Core/Services/ClientExceptionHandlerBase.cs)

**Purpose:** Base class for **client-side exception handlers**.

**Key Responsibilities:**
- Logs exceptions with telemetry context (file path, line number, member name, etc.)
- Determines how to display errors:
  - `ExceptionDisplayKind.Interrupting`: Message box
  - `ExceptionDisplayKind.NonInterrupting`: Snack bar
  - `ExceptionDisplayKind.None`: No UI (logs only, debugger break in Development)
- Automatically ignores `TaskCanceledException`, `OperationCanceledException`, `TimeoutException`

---

### 4. WebClientExceptionHandler

**Location:** [`src/Client/Boilerplate.Client.Web/Services/WebClientExceptionHandler.cs`](/src/Client/Boilerplate.Client.Web/Services/WebClientExceptionHandler.cs)

**Purpose:** Exception handler for **Blazor WebAssembly** (browser).

**Platform-Specific Behavior:**
- Inherits all behavior from `ClientExceptionHandlerBase`
- Can be extended to add **browser-specific** error tracking (e.g., Google Analytics)

---

### 5. MauiExceptionHandler

**Location:** [`src/Client/Boilerplate.Client.Maui/Services/MauiExceptionHandler.cs`](/src/Client/Boilerplate.Client.Maui/Services/MauiExceptionHandler.cs)

**Purpose:** Exception handler for **.NET MAUI** (Android, iOS, macOS).

**Platform-Specific Behavior:**
- Can integrate with **Firebase Crashlytics** for Android/iOS crash reporting
- Can integrate with **platform-specific error tracking** services
- Example:
  ```csharp
  protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
  {
      // Log to Firebase Crashlytics
      FirebaseCrashlytics.Instance.RecordException(exception);
      
      base.Handle(exception, displayKind, parameters);
  }
  ```

---

### 6. WindowsExceptionHandler

**Location:** [`src/Client/Boilerplate.Client.Windows/Services/WindowsExceptionHandler.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsExceptionHandler.cs)

**Purpose:** Exception handler for **Windows Forms Blazor Hybrid** app.

**Platform-Specific Behavior:**
- Can integrate with **Windows-specific error reporting** (e.g., Windows Error Reporting API)

---