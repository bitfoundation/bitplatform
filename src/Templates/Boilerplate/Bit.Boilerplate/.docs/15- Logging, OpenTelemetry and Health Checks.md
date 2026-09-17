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
public static readonly UpDownCounter<long> ActiveConversations =
    Meter.Current.CreateUpDownCounter<long>(
        "chatbot.active_conversations",
        "{conversation}",
        "Text chats open in the AI chat panel right now.");

// Increment when operation starts
ActiveConversations.Add(1);

try
{
    // Your long-running operation here
    await ProcessConversation();
}
finally
{
    // Decrement when operation completes
    ActiveConversations.Add(-1);
}
```

This pattern is used in `Features/Chatbot/ChatbotMetrics.cs` to track active chatbot conversations and voice calls in real-time, which can be monitored in the Aspire Dashboard, Azure Application Insights, or other observability tools. Name instruments the OpenTelemetry way: a lowercase `area.thing` name, `active_` for a count of things in progress, and the unit in curly braces (`{conversation}`, `{call}`).

### AI token usage

Every `IChatClient`, embedding generator and speech client registered with `.UseOpenTelemetry()` records `gen_ai.client.token.usage` (meter `Experimental.Microsoft.Extensions.AI`) with `gen_ai.token.type` set to `input` or `output`. `ChatbotMetrics` adds what those clients can't see to the same instrument: voice calls (`gen_ai.operation.name` = `realtime`), and the `input_text`, `input_audio`, `input_image`, `input_cached`, `output_text`, `output_audio` and `output_reasoning` split the bill depends on. Token types overlap (`input_cached` is part of `input_text`), so filter by one `gen_ai.token.type` before summing.

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
3. **`/healthz`** - the detailed report behind the **Operations** page (Management menu). It keeps the HealthChecks UI
   format and adds each check's failure status, timeout and full exception, plus the time of the report. It needs the
   `AppFeatures.System.Operations_View` feature, which only global admins have, and it always answers 200: the status is
   in the body.

`/health` and `/alive` are mapped in **every** environment and are **anonymous**, and they answer with one word.
`/healthz`, which names every dependency and carries each failure's exception (possibly a connection string), is
mapped everywhere too, but behind its feature and never cached. Adding health check endpoints to a non-development
deployment has security implications (see <https://aka.ms/dotnet/aspire/healthchecks>): decide deliberately whether to
expose `/health` publicly or to restrict it to your load balancer's network, because an anonymous caller can drive the
work behind it. `/health` and `/alive` responses are output-cached for 10 seconds, but that cache does not apply to a
failing (non-200) response.

The **Operations** page (`/operations`) is where a deployment is run from. It opens the **Hangfire dashboard** for a user
who also holds `AppFeatures.System.Jobs_Manage`, the claim
[`HangfireDashboardAuthorizationFilter`](/src/Server/Boilerplate.Server.Api/Infrastructure/RequestPipeline/HangfireDashboardAuthorizationFilter.cs)
checks, and only in a browser app served by the server: a hybrid app would open the url in the system browser, which
shares no cookie jar with its web view. The button refreshes the access token, calls `IUserController.UpdateSession`,
which writes the `access_token` cookie with the token's own expiry, and then opens `/hangfire`, a plain browser
navigation for which that cookie is the only credential.

The page also reads `/healthz`, refreshes it on an interval you pick, and keeps each
check's recent probes while it is open. It shows how long each check took against its timeout, which checks take the
instance out of rotation when they fail, when a cached check really ran, and each failure's details. It also lists
what no check can prove (push notification, email and SMS delivery, social sign in, ...) and how to verify each. Push,
email and SMS have a button that sends a test message through `IDiagnosticController`: the push to the current device's
subscription, the email and the SMS, which need `Operations_View` too, to the signed-in user's own address and phone number, if the
account has them. Its catalog (`OperationsPage.HealthChecks.cs`) gives each registration name a title, a group and a description; add
a new check there to give it the same treatment.

### Registered Checks

`AddDefaultHealthChecks` contributes the disk-space check (at least **2 GB** free), which is the only one tagged
`"live"`.

`AddServerApiHealthChecks` adds checks that report **Unhealthy** when they fail:
- **`AppDbContext`**: the database.
- **`hangfire`**: at least one running Hangfire server.
- **`appCertificate`**: the AppCertificate's validity period. It reports Degraded within 30 days of expiry and Unhealthy
  once the certificate has expired. Token signing and Data Protection keep using an expired certificate without complaint,
  so this check is the only thing that reports it.

It also adds checks for remote dependencies. Each one has a timeout and reports **Degraded** rather than Unhealthy, so a
provider outage does not take an otherwise healthy instance out of the load balancer:
- **`userProfileImages`**: the blob storage.
- **`smtp`**: connects, runs STARTTLS when `EnableSsl` is on, signs in when credentials are set, then quits without sending.
- **`sms`**: fetches the Twilio account and requires it to be active.
- **`keycloakIdentity`**: reads Keycloak's discovery document.
- **`cloudflare`**: purges a cache tag nothing carries, through the same call content changes use.
- **`entraId`**: reads the tenant's discovery document, then asks for an app-only token, sending the secret only to an
  https token endpoint on the authority's own origin. Only a token, `invalid_scope` or `invalid_resource` passes, so
  `unauthorized_client` (unknown client id), `invalid_client` (wrong secret) and throttling all fail the check.
- **`appleSignIn`**: signs the client secret with `AppleAuthKey.p8` (so a missing or broken key fails the check) and
  makes sure Apple is reachable. Apple checks the authorization code before the client, so the secret itself can't be
  verified.
- **`reCaptcha`**: makes sure siteverify answers. Google checks the response before the secret, so a wrong secret is not
  detected.
- **`azureSignalR`**: calls the service's health API, and with an `AccessKey` it also makes a signed user lookup. Only
  200 or 404 passes; a wrong key answers 401.
- **`firebase`** and **`apns`**: send to a device token that does not exist. Firebase and APNs reject the token only
  after they accept the credentials, so a token error means the check passed.
- **`aiChat`**: gets a real answer (at most 16 output tokens) from an Agent Framework agent
  built on the app's `IChatClient`, so it also catches exhausted quota or billing.
- **`aiEmbedding`**: embeds one word with the app's embedding generator, whichever provider is configured (OpenAI
  compatible or HuggingFace).
- **`aiSpeechToText`**: transcribes half a second of silence that the check generates.
- **`aiTextToSpeech`**: reads "OK" aloud with the configured voice, so a wrong voice fails the check too.
- **`aiRealtime`**: a call needs the browser's WebRTC offer, so this check mints a client secret (`realtime/client_secrets`)
  for the call's session instead. That request is free, and the provider still validates the key, model, voice and
  transcription settings.

Every AI check sends a real, minimal request through the same client the feature itself uses. A key, model, voice or
quota problem therefore shows up in the check the same way it would in the feature. Providers reject an invalid key
(401) before they look at the rest of the request.

In development, `sms`, `keycloakIdentity`, `cloudflare`, `entraId`, `appleSignIn`, `azureSignalR`, `firebase`, `apns`
and the AI checks are registered only when their settings are present. In any other environment they are always
registered, and one without settings reports Degraded with "... is not configured". A deployment that ships the code
for a dependency but leaves it unconfigured is most likely a mistake. If your project doesn't use one of them, remove
its code, check included.

Google, GitHub, Twitter and Facebook sign-in can't be verified without a user signing in, so they have no check. Outside
development, `googleSignIn`, `gitHubSignIn`, `twitterSignIn` and `facebookSignIn` appear only when their settings are
missing, reporting Degraded the same way.

`smtp`, `firebase`, `apns`, `cloudflare`, the sign-in checks, `reCaptcha` and the AI checks are registered with
`AddCachedCheck`, which reuses a healthy result for 5 minutes (1 minute for `azureSignalR`). Probes that arrive together share one run. A
failing result is never reused, so each probe checks again and a recovery shows up right away. This keeps load balancer
probes from turning into paid calls or repeated sign-ins.

With Redis enabled, Aspire registers `StackExchange.Redis_redis-persistent` (Unhealthy) and
`StackExchange.Redis_redis-cache`. The template changes `redis-cache` to Degraded, because FusionCache keeps serving from
memory without it. With a standalone API, `Server.Web` also checks the API's `/alive` endpoint (`serverApi`, Degraded).

Anything you add that a request path genuinely depends on should report Unhealthy; anything external should not.

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
