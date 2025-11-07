# Stage 15: Logging, OpenTelemetry and Health Checks

Welcome to Stage 15! In this stage, you'll learn about the comprehensive logging, monitoring, and health check system built into this Boilerplate project. This system provides production-ready observability from day one.

---

## 1. ILogger: Standard .NET Logging

### What is ILogger?

`ILogger` is the standard .NET logging interface used throughout the project for recording errors, warnings, informational messages, and debugging information.

### Basic Usage

In any component or service that inherits from `AppComponentBase` or `AppControllerBase`, you automatically have access to `Logger`:

**Example from a Controller:**
```csharp
[HttpGet]
public async Task<string> PerformDiagnostics(CancellationToken cancellationToken)
{
    Logger.LogInformation("Performing diagnostics check");
    
    try
    {
        // Your code here
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error during diagnostics");
        throw;
    }
}
```

**Example from a Blazor Component:**
```csharp
protected override async Task OnInitAsync()
{
    Logger.LogInformation("Component initialized");
    await base.OnInitAsync();
}
```

### Log Levels

The project uses standard .NET log levels:
- **Trace**: Very detailed diagnostic information
- **Debug**: Debugging information
- **Information**: General informational messages
- **Warning**: Warning messages for potentially harmful situations
- **Error**: Error messages for failures
- **Critical**: Critical failures requiring immediate attention

---

## 2. Activity and AppActivitySource: Tracking Operations

### What is AppActivitySource?

[`AppActivitySource`](/src/Shared/Services/AppActivitySource.cs) is a centralized OpenTelemetry source for tracking operations (activities) and metrics in your application. It provides two key objects:

```csharp
public class AppActivitySource
{
    // For tracking distributed traces and activities
    public static readonly ActivitySource CurrentActivity = new("Boilerplate", ...);
    
    // For tracking custom metrics (counters, histograms, gauges)
    public static readonly Meter CurrentMeter = new("Boilerplate", ...);
}
```

### Real-World Example 1: Histogram Metrics

**Location:** [`/src/Server/Boilerplate.Server.Api/Controllers/AttachmentController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/AttachmentController.cs)

This example tracks how long it takes to resize and save images:

```csharp
// Define the histogram metric at class level
private static readonly Histogram<double> updateResizeDurationHistogram = 
    AppActivitySource.CurrentMeter.CreateHistogram<double>(
        "attachment.resize_duration", 
        "ms", 
        "Elapsed time to resize and persist an uploaded image"
    );

// Use it to record measurements
public async Task UploadAttachment(...)
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    
    // Resize the image
    using MagickImage sourceImage = new(file.OpenReadStream());
    sourceImage.Resize(new MagickGeometry(width, height));
    await blobStorage.WriteAsync(attachment.Path, sourceImage.ToByteArray(MagickFormat.WebP));
    
    // Record the duration with a tag/dimension
    updateResizeDurationHistogram.Record(
        stopwatch.Elapsed.TotalMilliseconds, 
        new KeyValuePair<string, object?>("kind", kind.ToString())
    );
}
```

**Why is this useful?**
- You can see how long image resizing takes in production
- You can identify performance bottlenecks
- You can track changes over time
- The `kind` dimension lets you see performance per image type (profile pictures vs product images)

### Real-World Example 2: UpDownCounter Metrics

**Location:** [`/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs)

This example tracks the number of ongoing AI chatbot conversations:

```csharp
// Define the counter at class level
private static readonly UpDownCounter<long> ongoingConversationsCount = 
    AppActivitySource.CurrentMeter.CreateUpDownCounter<long>(
        "appHub.ongoing_conversations_count", 
        "Number of ongoing conversations in the chatbot hub."
    );

// Increment when conversation starts
ongoingConversationsCount.Add(1);

// Decrement when conversation ends
ongoingConversationsCount.Add(-1);
```

**Why is this useful?**
- Monitor how many active AI conversations are running
- Track resource usage in real-time
- Set up alerts when too many conversations are active
- Understand usage patterns

### Types of Metrics You Can Create

1. **Counter**: A value that only increases (e.g., total requests)
2. **UpDownCounter**: A value that can increase or decrease (e.g., active connections)
3. **Histogram**: Records a distribution of values (e.g., request durations)
4. **ObservableGauge**: Reports the current value of something (e.g., memory usage)

---

## 3. Logging Configuration

### Main Configuration File

**Location:** [`/src/Shared/appsettings.json`](/src/Shared/appsettings.json)

The project supports multiple logging providers, each with its own configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None"
    },
    "Console": {
      "LogLevel": {
        "Default": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "OpenTelemetry": {
      "LogLevel": {
        "Default": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Boilerplate.Client.Core.Services.AuthManager": "Information"
      }
    },
    "DiagnosticLogger": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore*": "Warning"
      }
    },
    "Sentry": {
      "Dsn": "",
      "LogLevel": {
        "Default": "Warning"
      }
    },
    "ApplicationInsights": {
      "ConnectionString": null,
      "LogLevel": {
        "Default": "Warning"
      }
    }
  }
}
```

### Environment-Specific Overrides

**Development:** [`/src/Shared/appsettings.Development.json`](/src/Shared/appsettings.Development.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"  // More verbose in development
    },
    "DiagnosticLogger": {
      "LogLevel": {
        "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware": "None"
      }
    }
  }
}
```

### Why Multiple Providers?

Each logging provider serves a different purpose:
- **Console**: For local development debugging (terminal/logcat output)
- **OpenTelemetry**: For structured logs in production, can export to various backends
- **DiagnosticLogger**: In-app logging for troubleshooting user issues (explained below)
- **Sentry**: Error tracking and monitoring service (optional)
- **Application Insights**: Azure monitoring service (optional)
- **Debug**: Visual Studio debug output window
- **EventLog**: Windows Event Log (Windows apps only)
- **EventSource**: For ETW (Event Tracing for Windows)

---

## 4. In-App Diagnostic Logger: The Troubleshooting Powerhouse

### What is the Diagnostic Logger?

The Diagnostic Logger is one of the most powerful features for troubleshooting issues. It stores logs **in the client device's memory** and can be viewed directly in the application UI.

### How to Access It

You can open the diagnostic modal in **three ways**:

1. **Keyboard Shortcut**: Press `Ctrl + Shift + X`
2. **Click 7 times** on the spacer in the header (implemented in [`DiagnosticSpacer.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/Header/DiagnosticSpacer.razor.cs))
3. **Browser Console**: Run `App.showDiagnostic()` in the browser's developer console

### Key Features

**Location:** [`/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor)

1. **View All Logs**: See all client-side logs with timestamps, categories, and messages
2. **Filter by Level**: Show only errors, warnings, etc.
3. **Filter by Category**: Focus on specific parts of the application
4. **Search**: Find logs using text search or regex
5. **Copy Logs**: Copy individual logs or all telemetry data
6. **Real-time Updates**: New logs appear automatically

### Developer Experience Benefits

During development, the Diagnostic Modal shows **both client AND server logs** for a seamless debugging experience:

**From the prompt instructions:**
> Diagnostic modal shows logs for both server and client side during development for better developer experience, but in production/staging, only client logs are shown for security reasons.

### Production/Support Benefits

This is extremely useful for support staff and troubleshooting user issues:

1. **Remote Troubleshooting**: Support staff with the right permissions can view user logs in real-time via SignalR
2. **No Server Access Needed**: Logs are captured on the client device
3. **More Verbose Than Production Loggers**: While production loggers (Sentry, App Insights) default to `Warning` level to reduce costs, DiagnosticLogger defaults to `Information` level

**See:** [`SharedPubSubMessages.cs`](/src/Shared/Services/SharedPubSubMessages.cs)
```csharp
/// <summary>
/// Allows super admins and support staff to retrieve all diagnostic logs for active user sessions.
/// In contrast to production loggers (e.g., Sentry, AppInsights), which use a Warning level by default 
/// (except for specific categories at Information level) to reduce costs,
/// the diagnostic logger defaults to Information level to capture all logs, stored solely in the 
/// client device's memory.
/// Uploading these logs for display in the support staff's diagnostic modal log viewer aids in 
/// pinpointing the root cause of user issues during live troubleshooting.
/// Another benefit of having this feature is in dev environment when you wanna see your Android, 
/// iOS logs on your desktop wide screen.
/// </summary>
public const string UPLOAD_DIAGNOSTIC_LOGGER_STORE = nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE);
```

### How It Works Internally

**Provider:** [`/src/Client/Boilerplate.Client.Core/Services/DiagnosticLog/DiagnosticLoggerProvider.cs`](/src/Client/Boilerplate.Client.Core/Services/DiagnosticLog/DiagnosticLoggerProvider.cs)

```csharp
[ProviderAlias("DiagnosticLogger")]
public partial class DiagnosticLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticLogger()
        {
            Category = categoryName
        };
    }
}
```

**Logger:** [`/src/Client/Boilerplate.Client.Core/Services/DiagnosticLog/DiagnosticLogger.cs`](/src/Client/Boilerplate.Client.Core/Services/DiagnosticLog/DiagnosticLogger.cs)

```csharp
public partial class DiagnosticLogger : ILogger
{
    // Static store shared across all logger instances
    public static ConcurrentQueue<DiagnosticLogDto> Store { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
                           Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) is false) return;

        // Limit to 1000 most recent logs to prevent memory issues
        if (Store.Count >= 1_000)
        {
            Store.TryDequeue(out var _);
        }

        Store.Enqueue(new DiagnosticLogDto
        {
            CreatedOn = DateTimeOffset.Now,
            Level = logLevel,
            Message = formatter(state, exception),
            Category = Category,
            ExceptionString = exception?.ToString(),
            State = currentState?.ToDictionary(i => i.Key, i => i.Value?.ToString())
        });
    }
}
```

### Permission Requirements

To view other users' diagnostic logs, users need the `AppFeatures.System.ManageLogs` feature/permission:

**See:** [`AppFeatures.cs`](/src/Shared/Services/AppFeatures.cs)
```csharp
public class AppFeatures
{
    public class System
    {
        /// <summary>
        /// Upload and view diagnostic logger store for troubleshooting
        /// </summary>
        public const string ManageLogs = "2.0";
    }
}
```

---

## 5. Easy Integration with Sentry and Azure Application Insights

The project makes it incredibly easy to integrate with professional monitoring services:

### Sentry Integration

**To Enable Sentry:**
1. Sign up at [sentry.io](https://sentry.io)
2. Add your DSN to [`/src/Shared/appsettings.json`](/src/Shared/appsettings.json):

```json
{
  "Logging": {
    "Sentry": {
      "Dsn": "https://your-dsn@sentry.io/project-id"
    }
  }
}
```

That's it! All logs automatically flow to Sentry on all platforms (Web, MAUI, Windows).

### Azure Application Insights Integration

**To Enable Application Insights:**
1. Create an Application Insights resource in Azure
2. Add the connection string to [`/src/Shared/appsettings.json`](/src/Shared/appsettings.json):

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://..."
  }
}
```

All logs and metrics automatically flow to Application Insights!

### Platform-Specific Configuration

The logging is configured per platform:

**Web (Blazor WebAssembly):** [`/src/Client/Boilerplate.Client.Web/Program.Services.cs`](/src/Client/Boilerplate.Client.Web/Program.Services.cs)
**MAUI:** [`/src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs`](/src/Client/Boilerplate.Client.Maui/MauiProgram.Services.cs)
**Windows:** [`/src/Client/Boilerplate.Client.Windows/Program.Services.cs`](/src/Client/Boilerplate.Client.Windows/Program.Services.cs)

Each calls the shared configuration method:

```csharp
builder.Logging.ConfigureLoggers(configuration);
```

**See:** [`ILoggingBuilderExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/ILoggingBuilderExtensions.cs)

```csharp
public static ILoggingBuilder ConfigureLoggers(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
{
    loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

    if (AppEnvironment.IsDevelopment())
    {
        loggingBuilder.AddDebug();
    }

    if (!AppPlatform.IsBrowser)
    {
        loggingBuilder.AddConsole(options => configuration.GetRequiredSection("Logging:Console").Bind(options));
    }

    loggingBuilder.AddDiagnosticLogger();

    loggingBuilder.AddSentry(options =>
    {
        options.Debug = AppEnvironment.IsDevelopment();
        options.Environment = AppEnvironment.Current;
        configuration.GetRequiredSection("Logging:Sentry").Bind(options);
    });

    return loggingBuilder;
}
```

---

## 6. Aspire Dashboard: Visualize Everything

If you have .NET Aspire enabled in your project (see [`/src/Server/Boilerplate.Server.AppHost`](/src/Server/Boilerplate.Server.AppHost)), you get access to the **Aspire Dashboard**:

- **All Logs**: View structured logs from all services
- **All Metrics**: See histograms, counters, gauges in real-time
- **Distributed Traces**: Track requests across multiple services
- **Resource Monitoring**: CPU, memory, network usage

**To Access:**
1. Run the project with Aspire enabled
2. Navigate to the Aspire Dashboard URL (typically shown in the console on startup)

All your custom metrics created with `AppActivitySource.CurrentMeter` automatically appear in the dashboard!

---

## 7. 🚨 CRITICAL WARNING: Don't Over-Engineer Logging

### The Common Mistake

Many developers want to add:
- **Serilog** (unnecessary, OpenTelemetry is better)
- **Direct Application Insights SDK calls** (unnecessary, use ILogger)
- **Custom logging frameworks** (unnecessary, Microsoft.Extensions.Logging is sufficient)

### Why You Shouldn't

If you're considering adding these, **you probably don't understand OpenTelemetry or Microsoft.Extensions.Logging**.

**Everything is already optimally configured:**
- ✅ Multiple logging providers
- ✅ OpenTelemetry integration
- ✅ Structured logging
- ✅ Metrics and traces
- ✅ Easy third-party integration
- ✅ Cross-platform support
- ✅ Performance optimized
- ✅ Production-ready

### The Right Approach

**Just use:**
- `ILogger` for all logging
- `AppActivitySource.CurrentMeter` for custom metrics
- `AppActivitySource.CurrentActivity` for custom traces (if needed)

The framework handles everything else automatically:
- Log levels per provider
- Structured logging
- Correlation IDs
- Exception tracking
- Performance monitoring

---

## 8. Health Checks

### What Are Health Checks?

Health checks allow you to monitor the health of your application and its dependencies. They're commonly used by:
- Load balancers
- Kubernetes
- Azure App Service
- Monitoring tools

### Available Endpoints

**Location:** [`/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationExtensions.cs)

The project provides three health check endpoints:

```csharp
// 1. /health - All health checks must pass
healthChecks.MapHealthChecks("/health");

// 2. /alive - Only "live" tagged health checks must pass
//    Used to determine if the app is alive (not deadlocked)
healthChecks.MapHealthChecks("/alive", new()
{
    Predicate = static r => r.Tags.Contains("live")
});

// 3. /healthz - Detailed JSON response with all checks
healthChecks.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Built-in Health Checks

**Location:** [`/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs`](/src/Server/Boilerplate.Server.Shared/Extensions/WebApplicationBuilderExtensions.cs)

```csharp
public static IHealthChecksBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
{
    return builder.Services.AddHealthChecks()
        .AddDiskStorageHealthCheck(opt => 
            opt.AddDrive(
                Path.GetPathRoot(Directory.GetCurrentDirectory())!, 
                minimumFreeMegabytes: 5 * 1024  // Alert if less than 5GB free
            ), 
            tags: ["live"]
        );
}
```

### Response Caching for Health Checks

Health check responses are cached for 10 seconds to reduce load:

```csharp
builder.Services.AddOutputCache(configureOptions: static caching =>
    caching.AddPolicy("HealthChecks",
        build: static policy => policy.Expire(TimeSpan.FromSeconds(10)))
);

// Applied to endpoints
healthChecks.CacheOutput("HealthChecks");
```

### Testing Health Checks

**Try these URLs when your app is running:**
- `https://localhost:5001/health` - Simple healthy/unhealthy response
- `https://localhost:5001/alive` - Liveness probe
- `https://localhost:5001/healthz` - Detailed JSON with all health information

### Adding Custom Health Checks

You can easily add more health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddDiskStorageHealthCheck(...)
    .AddDbContextCheck<AppDbContext>()  // Check database connectivity
    .AddUrlGroup(new Uri("https://api.example.com"), "External API")  // Check external API
    .AddCheck("MyCustomCheck", () => 
    {
        // Your custom health logic
        return HealthCheckResult.Healthy("Everything is fine!");
    }, tags: ["custom"]);
```

---

## Summary

In Stage 15, you learned about:

1. **ILogger**: Standard .NET logging for errors, warnings, and information
2. **AppActivitySource**: Custom metrics and traces using OpenTelemetry
3. **Logging Configuration**: Multiple providers configured via `appsettings.json`
4. **Diagnostic Logger**: In-app troubleshooting tool accessible via `Ctrl+Shift+X`
5. **Third-Party Integration**: Easy setup for Sentry and Application Insights
6. **Aspire Dashboard**: Visualize logs, metrics, and traces
7. **Health Checks**: Monitor application health at `/health`, `/alive`, and `/healthz`

### Real-World Examples You Saw

- **Histogram**: Tracking image resize duration in `AttachmentController`
- **UpDownCounter**: Counting active chatbot conversations in `AppHub`
- **Diagnostic Modal**: Full-featured log viewer for troubleshooting
- **Health Checks**: Disk space monitoring

### Key Takeaways

✅ **Use `ILogger`** for all logging needs
✅ **Use `AppActivitySource.CurrentMeter`** for custom metrics
✅ **Don't over-engineer** - the built-in system is production-ready
✅ **Leverage the Diagnostic Modal** for troubleshooting
✅ **Set up Sentry or App Insights** with just a connection string
✅ **Monitor health** with the built-in endpoints

---
