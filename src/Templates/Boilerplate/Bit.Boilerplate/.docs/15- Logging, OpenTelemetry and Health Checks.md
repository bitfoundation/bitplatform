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
7. [Critical Warning About Sensitive Data](#critical-warning-about-sensitive-data)
8. [Health Checks](#health-checks)

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

This is implemented in [`src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs):

```csharp
/// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
[Authorize(Policy: CustomPolicies.ViewUserSession)]
public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext)
{
    var userId = await dbContext.UserSessions.Where(us => us.Id == userSessionId)
                                             .Select(us => us.UserId)
                                             .SingleOrDefaultAsync();

    if (userId is null)
        throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserSessionCouldNotBeFound)]);

    return await hubConnection.InvokeAsync<DiagnosticLogDto[]>(nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE));
}
```

---

## 5. Integration with Sentry and Azure Application Insights

The project is **pre-configured** for easy integration with popular logging providers.

### Sentry Integration

**Sentry** is a production error tracking service. Configuration in `appsettings.json`:

```json
"Sentry": {
  "Dsn": "", // Add your Sentry DSN here
  "SendDefaultPii": true,
  "EnableScopeSync": true,
  "LogLevel": {
    "Default": "Warning"
  }
}
```

### Azure Application Insights Integration

**Application Insights** provides comprehensive telemetry and monitoring. Configuration:

```json
"ApplicationInsights": {
  "ConnectionString": null // Add your connection string here
}
```

### How It Works

The OpenTelemetry configuration automatically exports to Application Insights if a connection string is provided:

From [`src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs):

```csharp
private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    var appInsightsConnectionString = string.IsNullOrWhiteSpace(builder.Configuration["ApplicationInsights:ConnectionString"]) is false 
        ? builder.Configuration["ApplicationInsights:ConnectionString"] 
        : null;

    if (appInsightsConnectionString is not null)
    {
        builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            builder.Configuration.Bind("ApplicationInsights", options);
        }).AddAzureMonitorProfiler();
    }

    return builder;
}
```

### OpenTelemetry Configuration

The project tracks:

**Metrics:**
- ASP.NET Core instrumentation (HTTP request metrics)
- HTTP client instrumentation
- Runtime instrumentation (GC, thread pool, etc.)
- Custom metrics via `Meter.Current`

**Tracing:**
- ASP.NET Core requests (excluding static files and health checks)
- HTTP client calls
- Entity Framework Core queries (excluding Hangfire queries)
- Hangfire background jobs
- Custom activities via `ActivitySource.Current`

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

1. **`/health`** - All health checks must pass
2. **`/alive`** - Only checks tagged with "live" must pass
3. **`/healthz`** - Detailed health report (UI format)

### Health Check Implementation

From [`src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationExtensions.cs):

```csharp
public static WebApplication MapAppHealthChecks(this WebApplication app)
{
    // Adding health checks endpoints to applications in non-development environments has security implications.
    // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
    if (app.Environment.IsDevelopment())
    {
        var healthChecks = app.MapGroup("");

        healthChecks.CacheOutput("HealthChecks");

        // All health checks must pass for app to be considered ready
        healthChecks.MapHealthChecks("/health");

        // Only health checks tagged with "live" must pass
        healthChecks.MapHealthChecks("/alive", new()
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        // Detailed health report with UI
        healthChecks.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }

    return app;
}
```

### Default Health Checks

From [`src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs):

```csharp
public static IHealthChecksBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    return builder.Services.AddHealthChecks()
        .AddDiskStorageHealthCheck(opt => 
            opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, 
            minimumFreeMegabytes: 5 * 1024), 
            tags: ["live"]);
}
```

This checks that at least **5GB of free disk space** is available.

### Custom Health Check Example

The project includes a custom health check for storage in [`src/Server/Boilerplate.Server.Api/Services/AppStorageHealthCheck.cs`](/src/Server/Boilerplate.Server.Api/Services/AppStorageHealthCheck.cs):

```csharp
/// <summary>
/// Checks underlying S3, Azure blob storage, or local file system storage is healthy.
/// </summary>
public partial class AppStorageHealthCheck : IHealthCheck
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private ServerApiSettings settings = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await blobStorage.ExistsAsync(settings.UserProfileImagesDir, cancellationToken);

            return HealthCheckResult.Healthy("Storage is healthy");
        }
        catch (Exception exp)
        {
            return HealthCheckResult.Unhealthy("Storage is unhealthy", exp);
        }
    }
}
```

### Built-in Health Checks

The project automatically configures health checks for all infrastructure components registered in the `AddServerApiProjectServices` method, including:
- Database connectivity
- Disk storage availability (minimum 5GB free)
- Blob storage (S3, Azure Blob, or local file system)

---