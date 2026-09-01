# Stage 15: Logging, OpenTelemetry and Health Checks

Welcome to Stage 15! In this stage, you'll learn about the comprehensive logging, observability, and health monitoring infrastructure built into the Boilerplate project.

---

## Table of Contents

1. [ILogger for Errors, Warnings, and Information](#ilogger-for-errors-warnings-and-information)
2. [Activity and Meter for Tracking Operations](#activity-and-meter-for-tracking-operations)
3. [Logging Configuration](#logging-configuration)
4. [In-App Diagnostic Logger](#in-app-diagnostic-logger)
5. [Integration with Sentry and Azure Application Insights](#integration-with-sentry-and-azure-application-insights)
6. [Aspire Dashboard](#aspire-dashboard)
7. [Health Checks](#health-checks)

---

## 1. ILogger for Errors, Warnings, and Information

The project uses **`ILogger<T>`** from `Microsoft.Extensions.Logging` for structured logging throughout the application.

### Basic Usage

```csharp
[AutoInject] private ILogger<MyService> logger = default!;

public async Task ProcessData()
{
    logger.LogInformation("Processing started");
    
    try
    {
        // Your code here
        logger.LogWarning("Something unusual happened");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process data");
    }
}
```

### Structured Logging with Scopes

For adding contextual information to logs, use **`BeginScope`**:

```csharp
var data = new Dictionary<string, object?>
{
    { "UserId", userId },
    { "OrderId", orderId },
    { "Culture", CultureInfo.CurrentUICulture.Name }
};

using var scope = logger.BeginScope(data);
logger.LogError(exception, "Order processing failed");
```

---

## 2. Activity and Meter for Tracking Operations

For tracking **operation count and duration**, the project uses **OpenTelemetry's ActivitySource**.

### ActivitySource

### Using Activities to Track Operations

```csharp
using var activity = ActivitySource.Current.StartActivity("ProcessOrder");

try
{
    // Your operation here
    activity?.SetTag("orderId", orderId);
    activity?.SetTag("customerId", customerId);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### Using Meters for Count Metrics
For tracking **count metrics** (e.g., number of ongoing operations), use **OpenTelemetry's Meter**:

```csharp
// Define a counter at class level
private static readonly UpDownCounter<long> ongoingConversationsCount = 
    Meter.Current.CreateUpDownCounter<long>(
        "appHub.ongoing_conversations_count", 
        "Number of ongoing conversations in the chatbot hub.");

// Increment when operation starts
ongoingConversationsCount.Add(1);

try
{
    // Your long-running operation here
    await ProcessConversation();
}
finally
{
    // Decrement when operation completes
    ongoingConversationsCount.Add(-1);
}
```

This pattern is used in `AppHub.Chatbot.cs` to track the number of active chatbot conversations in real-time, which can be monitored in the Aspire Dashboard, Azure Application Insights, or other observability tools.

### Benefits

- **Duration Tracking**: Automatically measures how long operations take
- **Distributed Tracing**: Tracks requests across multiple services
- **Performance Insights**: Identifies bottlenecks and slow operations
- **Visualizations**: View traces in Aspire Dashboard, Application Insights, or other observability tools

---

## 3. Logging Configuration

The logging configuration is centralized in [`src/Shared/appsettings.json`](/src/Shared/appsettings.json).

### Configuration Structure

```json
{
  "ApplicationInsights": {
    "ConnectionString": null
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None"
    },
    "Sentry": {
      "Sentry_Comment": "https://docs.sentry.io/platforms/dotnet/guides/extensions-logging/",
      "Dsn": "",
      "SendDefaultPii": true,
      "EnableScopeSync": true,
      "LogLevel": {
        "Default": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "DiagnosticLogger": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore*": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    }
  }
}
```

### Key Configuration Sections

- **Default Log Level**: `Warning` - Only warnings and above are logged by default
- **EF Core Commands**: `Information` - Shows SQL queries in logs (useful for debugging)
- **Sentry**: Production error tracking with `Warning` level
- **DiagnosticLogger**: `Information` level for in-app diagnostics
- **Console**: Logs to device log/logcat on mobile platforms

---

## 4. In-App Diagnostic Logger

One of the **most useful troubleshooting features** in this project is the **Diagnostic Logger** - a custom in-memory logger that helps debug issues in real-time.

### What is the Diagnostic Logger?

The Diagnostic Logger is a custom `ILogger` implementation that:
- Stores logs **in memory** on the client device
- Defaults to **`Information` level** (captures more details than production loggers)
- Allows viewing logs directly in the application UI
- Can be accessed by support staff to troubleshoot user issues remotely

### Implementation

### Accessing the Diagnostic Modal

There are **three ways** to open the Diagnostic Modal:

1. **Click 7 times** on the spacer in the running app's header
2. **Press** `Ctrl+Shift+X` (keyboard shortcut)
3. **Run JavaScript** in browser dev tools: `App.showDiagnostic()`

### Diagnostic Modal UI

**Environment-Specific Behavior:**
- The diagnostic modal shows **client-side logs** from the in-memory `DiagnosticLogger.Store`
- This is useful for support staff who have remote access to a user's machine/device to troubleshoot issues

### Remote Troubleshooting

For **live support scenarios**, support staff can request diagnostic logs from a user's active session:

1. Support staff opens the users page and find the user
2. Clicks "View Diagnostic Logs" button
3. The server sends a SignalR message to the user's device
4. The device uploads its in-memory logs to the server
5. Support staff can view the logs in real-time

This is implemented in [`src/Server/Boilerplate.Server.Api/Infrastructure/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/Infrastructure/SignalR/AppHub.cs):

```csharp
/// <inheritdoc cref="SharedAppMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
[HubMethodName(SharedAppMessages.GetUserSessionLogs)]
public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext, [FromServices] IAuthorizationService authorizationService)
{
    var user = Context.GetHttpContext()!.User;

    if ((await authorizationService.AuthorizeAsync(user, AppFeatures.System.Logs_View)).Succeeded is false)
        throw new HubException(nameof(AppStrings.UnauthorizedException)).WithData("ConnectionId", Context.ConnectionId);

    // ... resolves the session's SignalRConnectionId, scoped to the caller's tenant, then:
    return await Clients.Client(connectionId).InvokeAsync<DiagnosticLogDto[]>(SharedAppMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted);
}
```

---

## 5. Supported telemetry platforms:

### 🖥️ Server Applications

* **Server.Api**

    Server.Api uses **OpenTelemetry** for distributed tracing and metrics.
    
    Support for Open Telemetry means that all telemetry platforms (Including but not limited to Sentry, Azure Application Insights, Datadog, New Relic etc.) can be used to collect and visualize logs, traces and metrics.

    **Sampling** is enabled to reduce costs: unknown/unhandled exceptions and slow activities are always captured (100%), while known/transient exceptions, warnings, and info logs are sampled at **5%**. See [`AppOpenTelemetryProcessor.cs`](../src/Server/Boilerplate.Server.Shared/Infrastructure/Services/AppOpenTelemetryProcessor.cs) and [`AppLoggingSampler.cs`](../src/Server/Boilerplate.Server.Shared/Infrastructure/Services/AppLoggingSampler.cs).

* **Server.Web**

    Server.Web uses **OpenTelemetry** for distributed tracing and metrics.

### 📱 Client Applications

* **Client.Windows**

    Client.Windows uses **OpenTelemetry** for distributed tracing and metrics, and uses Azure Application Insights JavaScript SDK for Blazor Hybrid WebView JavaScript errors, navigation tracking etc.

* **Client.Maui**

    Client.Maui uses **OpenTelemetry** for distributed tracing and metrics, and uses Azure Application Insights JavaScript SDK for Blazor Hybrid WebView JavaScript errors, navigation tracking etc.

* **Client.Web (Blazor WebAssembly)**

    Client.Web doesn't use Open Telemetry due to size constraints, but uses any Microsoft.Extensions.Logging implementations such as `Sentry.Extensions.Logging`

    `BlazorApplicationInsights` nuget package implements Microsoft.Extensions.Logging. It also tracks Browser JavaScript errors, navigations etc.

---

## 6. Aspire Dashboard

The **.NET Aspire Dashboard** provides a unified view of all logs, traces, and metrics.

### What is Aspire Dashboard?

The Aspire Dashboard is a web-based UI that displays:
- **Logs**: All logged messages from all services
- **Traces**: Distributed traces showing request flow across services
- **Metrics**: Performance metrics (CPU, memory, request rates, custom metrics)
- **Resources**: Overview of all running services and their health

### Accessing the Dashboard

When running the project with .NET Aspire (via `Boilerplate.Server.AppHost`), the dashboard is automatically available at:

```
https://localhost:2030
```

### Key Features

- **Real-time Updates**: See logs and traces as they happen
- **Advanced Filtering**: Filter logs by level, category, service, time range
- **Trace Visualization**: See how requests flow through your system
- **Performance Analysis**: Identify slow operations and bottlenecks

---

## 8. Health Checks

The project includes **health check endpoints** to monitor application health.

### Available Endpoints

1. **`/health`** - readiness. Runs every registered check, and returns 503 only when one of them reports Unhealthy.
2. **`/alive`** - liveness. Runs only the checks tagged `"live"`, which today is the disk-space check alone.
3. **`/healthz`** - detailed health report (UI format). **Development only.**

`/health` and `/alive` are mapped in **every** environment and are **anonymous** - only `/healthz`, which is the one
that reveals per-check details, is gated on Development. Adding health check endpoints to a non-development
deployment has security implications (see <https://aka.ms/dotnet/aspire/healthchecks>): decide deliberately whether to
expose `/health` publicly or to restrict it to your load balancer's network, because an anonymous caller can drive the
work behind it. Responses are output-cached for 10 seconds, but that cache does not apply to a failing (non-200)
response.

### Registered Checks

`AddDefaultHealthChecks` contributes the disk-space check (at least **5 GB** free), which is the only one tagged
`"live"`. `AddServerApiHealthChecks` adds the database, Hangfire, the user-profile-images blob storage and - when SMS
is configured - Twilio. The last two reach a remote dependency, so they carry a timeout and report **Degraded** rather
than Unhealthy: a storage or SMS-provider outage must not take an otherwise healthy instance out of the load balancer.
Anything you add that a request path genuinely depends on should report Unhealthy; anything external should not.

---

### AI Wiki: Answered Questions

Ask your own question [here](https://bitplatform.dev/ask)

---
