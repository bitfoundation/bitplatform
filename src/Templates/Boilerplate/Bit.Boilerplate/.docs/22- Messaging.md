# Stage 22: Messaging

Welcome to Stage 22! In this stage, you will learn about the comprehensive **messaging and real-time communication system** built into the Boilerplate project. This system combines **SignalR**, **Push Notifications**, and a **Pub/Sub pattern** to provide seamless communication between server and clients.

---

## 📋 Topics Covered

1. **SignalR Real-Time Communication**
   - SignalR Hub Architecture
   - Connection Management & Authentication
   - Server-to-Client Messaging
   - Client-to-Server Invocations

2. **Pub/Sub Messaging Pattern**
   - PubSubService Architecture
   - Client-Side vs Server-Side Messages
   - WeakReference-Based Subscriptions
   - Cross-Component Communication

3. **Push Notifications**
   - Platform-Specific Push Notification Services
   - Web Push (VAPID)
   - Native Push (Android, iOS, Windows, macOS)
   - Subscription Management

4. **Browser Notification API**
   - Bit.Butil.Notification Integration
   - Permission Management
   - Local Notifications

---

## 1. SignalR Real-Time Communication

### What is SignalR?

**SignalR** is ASP.NET Core's real-time communication library that enables bi-directional communication between server and clients. The Boilerplate project uses SignalR to:

- Send messages from server to specific clients, groups, or all clients
- Invoke client-side methods from the server
- Manage authentication state changes
- Coordinate user sessions across multiple devices/tabs

### AppHub - The SignalR Hub

The main SignalR hub is located at:

**Location**: [`src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs)

#### Hub Capabilities

The `AppHub` provides enhanced messaging capabilities beyond basic SignalR:

1. **`Clients.All()`**: Broadcasts to all SignalR connections (authenticated or not)
2. **`Clients.User(userId)`**: Sends messages to all tabs/apps for a specific user
3. **`Clients.Group("AuthenticatedClients")`**: Sends messages to all signed-in browser tabs and apps
4. **`Clients.Client(connectionId)`**: Targets a specific connection (e.g., a specific browser tab or app instance)

#### Authentication State Management

Each user session tracks its **SignalR connection ID** in the database. This enables powerful scenarios like:

**Example**: When an admin revokes a user session, the server can send a SignalR message directly to that specific browser tab or app:

```csharp
// From UserController.cs - RevokeSession method
await appHubContext.Clients.Client(userSession.SignalRConnectionId)
    .SendAsync(SignalREvents.SHOW_MESSAGE, message, null, cancellationToken);
```

This ensures the corresponding browser tab or app immediately:
- Clears its access/refresh tokens from storage
- Navigates to the sign-in page automatically

#### Key Methods in AppHub

**OnConnectedAsync()**: Called when a client connects

```csharp
public override async Task OnConnectedAsync()
{
    if (Context.GetHttpContext()?.ContainsExpiredAccessToken() is true)
        throw new HubException(nameof(AppStrings.UnauthorizedException))
            .WithData("ConnectionId", Context.ConnectionId);

    await ChangeAuthenticationStateImplementation(Context.User);
    await base.OnConnectedAsync();
}
```

**ChangeAuthenticationState()**: Updates authentication state while SignalR is connected

```csharp
public Task ChangeAuthenticationState(string? accessToken)
{
    ClaimsPrincipal? user = null;

    if (string.IsNullOrEmpty(accessToken) is false)
    {
        var bearerTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector;
        var accessTokenTicket = bearerTokenProtector.Unprotect(accessToken);
        user = accessTokenTicket!.Principal;
    }

    return ChangeAuthenticationStateImplementation(user);
}
```

This method is called from `AppClientCoordinator.cs` when the user signs in or out while the SignalR connection is active.

**GetUserSessionLogs()**: Retrieves diagnostic logs from a specific user session

```csharp
[Authorize(Policy = AppFeatures.System.ManageLogs)]
public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext)
{
    var userSessionSignalRConnectionId = await dbContext.UserSessions
        .Where(us => us.Id == userSessionId)
        .Select(us => us.SignalRConnectionId)
        .FirstOrDefaultAsync(Context.ConnectionAborted);

    if (string.IsNullOrEmpty(userSessionSignalRConnectionId))
        return [];

    return await Clients.Client(userSessionSignalRConnectionId)
        .InvokeAsync<DiagnosticLogDto[]>(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted);
}
```

This allows admins/support staff to retrieve diagnostic logs from active user sessions in real-time for troubleshooting.

### SignalR Hub Configuration

The SignalR hub is configured in [`src/Server/Boilerplate.Server.Api/Program.Middlewares.cs`](/src/Server/Boilerplate.Server.Api/Program.Middlewares.cs):

```csharp
app.MapHub<SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true);
```

**Key Features**:
- **AllowStatefulReconnects**: Enables stateful reconnection for better resilience
- **Azure SignalR Support**: The project is configured to support Azure SignalR Service for scalability

### Client-Side SignalR Connection

The SignalR connection is configured on the client side in:

**Location**: [`src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)

```csharp
var hubConnection = new HubConnectionBuilder()
    .WithStatefulReconnect()
    .AddJsonProtocol(options =>
    {
        foreach (var chain in sp.GetRequiredService<JsonSerializerOptions>().TypeInfoResolverChain)
        {
            options.PayloadSerializerOptions.TypeInfoResolverChain.Add(chain);
        }
    })
    .WithAutomaticReconnect(sp.GetRequiredService<IRetryPolicy>())
    .WithUrl(new Uri(absoluteServerAddressProvider.GetAddress(), "app-hub"), options =>
    {
        options.SkipNegotiation = false; // Required for Azure SignalR
        options.Transports = HttpTransportType.WebSockets;
        // Avoid enabling long polling or Server-Sent Events. Focus on resolving issues with WebSockets.
        // WebSockets should be enabled on services like IIS or Cloudflare CDN
        options.HttpMessageHandlerFactory = httpClientHandler => sp.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(httpClientHandler);
        options.AccessTokenProvider = async () =>
        {
            try
            {
                return await authManager.GetFreshAccessToken(requestedBy: nameof(HubConnection));
            }
            catch (ServerConnectionException) { } 
            return null;
        };
    })
    .Build();
```

**Key Features**:
- **WebSockets Only**: Uses WebSockets transport for better performance
- **Automatic Reconnection**: Reconnects automatically with retry policy
- **JWT Authentication**: Provides access token for authenticated connections
- **Azure SignalR Compatible**: Works with Azure SignalR Service

---

## 2. Pub/Sub Messaging Pattern

### What is PubSubService?

The **PubSubService** implements a publish/subscribe pattern that enables **decoupled communication** between different parts of the application. It can be used:

- Within Blazor components and pages
- Outside Blazor components (e.g., MAUI XAML pages)
- From JavaScript code using `window.postMessage`
- From server-side using SignalR

**Location**: [`src/Client/Boilerplate.Client.Core/Services/PubSubService.cs`](/src/Client/Boilerplate.Client.Core/Services/PubSubService.cs)

### Architecture

The PubSubService uses **WeakReference-based subscriptions** to prevent memory leaks:

```csharp
private readonly ConcurrentDictionary<string, List<WeakHandler>> handlers = [];
```

**WeakReference Benefit**: When a component is disposed, its subscription is automatically garbage collected without requiring explicit unsubscription (although explicit unsubscription is still recommended for deterministic cleanup).

### Publishing Messages

```csharp
public void Publish(string message, object? payload = null, bool persistent = false)
{
    if (handlers.TryGetValue(message, out var weakHandlers))
    {
        foreach (var weakHandler in weakHandlers.ToArray())
        {
            weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
    else if (persistent)
    {
        persistentMessages.Add((message, payload));
    }
}
```

**Persistent Messages**: If `persistent = true`, the message is stored and delivered to handlers that subscribe **after** the message was published. This is useful for scenarios where a component needs data that was published before it was created.

### Subscribing to Messages

```csharp
public Action Subscribe(string message, Func<object?, Task> handler)
{
    var weakHandler = new WeakHandler(handler);
    var weakHandlers = handlers.GetOrAdd(message, _ => []);
    weakHandlers.Add(weakHandler);

    // Deliver persistent messages immediately
    foreach (var (notHandledMessage, payload) in persistentMessages)
    {
        if (notHandledMessage == message)
        {
            weakHandler.Invoke(payload)?.ContinueWith(HandleException, TaskContinuationOptions.OnlyOnFaulted);
            persistentMessages.TryTake(out _);
        }
    }

    return () =>
    {
        var removedHandlersCount = weakHandlers.RemoveAll(wh => wh.Matches(handler));
    };
}
```

**Returns**: An `Action` to unsubscribe from the message.

### Message Types

#### SharedPubSubMessages

**Location**: [`src/Shared/Services/SharedPubSubMessages.cs`](/src/Shared/Services/SharedPubSubMessages.cs)

These messages are used for **server-to-client communication** through SignalR:

```csharp
public partial class SharedPubSubMessages
{
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
}

public static partial class SignalREvents
{
    public const string PUBLISH_MESSAGE = nameof(PUBLISH_MESSAGE);
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);
    public const string EXCEPTION_THROWN = nameof(EXCEPTION_THROWN);
}

public static partial class SignalRMethods
{
    public const string UPLOAD_DIAGNOSTIC_LOGGER_STORE = nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE);
}
```

**SignalREvents** are messages sent **from server to client**.

**SignalRMethods** are methods the server can **invoke on the client** (and wait for a response).

#### ClientPubSubMessages

**Location**: [`src/Client/Boilerplate.Client.Core/Services/ClientPubSubMessages.cs`](/src/Client/Boilerplate.Client.Core/Services/ClientPubSubMessages.cs)

These messages are for **client-only** pub/sub communication:

```csharp
public partial class ClientPubSubMessages : SharedPubSubMessages
{
    public const string SHOW_SNACK = nameof(SHOW_SNACK);
    public const string SHOW_MODAL = nameof(SHOW_MODAL);
    public const string CLOSE_MODAL = nameof(CLOSE_MODAL);
    public const string THEME_CHANGED = nameof(THEME_CHANGED);
    public const string OPEN_NAV_PANEL = nameof(OPEN_NAV_PANEL);
    public const string CLOSE_NAV_PANEL = nameof(CLOSE_NAV_PANEL);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);
    public const string IS_ONLINE_CHANGED = nameof(IS_ONLINE_CHANGED);
    public const string PAGE_DATA_CHANGED = nameof(PAGE_DATA_CHANGED);
    public const string ROUTE_DATA_UPDATED = nameof(ROUTE_DATA_UPDATED);
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);
    public const string AD_HAVE_TROUBLE = nameof(AD_HAVE_TROUBLE);
    public const string SOCIAL_SIGN_IN = nameof(SOCIAL_SIGN_IN);
    public const string FORCE_UPDATE = nameof(FORCE_UPDATE);
}
```

**Note**: `ClientPubSubMessages` inherits from `SharedPubSubMessages`, so it includes both client-only and server-to-client messages.

### Example: Server Publishing to Clients via SignalR

**Server-Side** (from `ProductController.cs`):

```csharp
private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
{
    // Publish to all authenticated clients
    await appHubContext.Clients.Group("AuthenticatedClients")
        .SendAsync(SignalREvents.PUBLISH_MESSAGE, 
                   SharedPubSubMessages.DASHBOARD_DATA_CHANGED, 
                   null, 
                   cancellationToken);
}
```

**Client-Side** (from `AppClientCoordinator.cs`):

```csharp
hubConnection.On<string, object?>(SignalREvents.PUBLISH_MESSAGE, async (message, payload) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
    PubSubService.Publish(message, payload);
});
```

When the server sends `PUBLISH_MESSAGE` through SignalR, the client automatically publishes it through `PubSubService`, notifying all subscribed components.

### Example: Client-Side Pub/Sub

**Publishing** (from `ProfileSection.razor.cs`):

```csharp
PubSubService.Publish(ClientPubSubMessages.PROFILE_UPDATED, CurrentUser);
```

**Subscribing** (from `MainLayout.razor.cs`):

```csharp
unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, async payload =>
{
    CurrentUser = payload is JsonElement jsonElement && jsonElement.ValueKind is not JsonValueKind.Undefined
        ? jsonDocument.Deserialize(jsonSerializerOptions.GetTypeInfo<UserDto>())! 
        : (UserDto)payload!;
    await InvokeAsync(StateHasChanged);
}));
```

This pattern allows components to communicate without direct references to each other, maintaining **loose coupling**.

---

## 3. AppClientCoordinator - Orchestrating Everything

The **AppClientCoordinator** component is responsible for coordinating all messaging services when the application starts.

**Location**: [`src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs`](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs)

### Key Responsibilities

1. **Initialize SignalR Connection**
2. **Subscribe to SignalR Events**
3. **Manage Authentication State Propagation**
4. **Handle Push Notification Subscriptions**
5. **Coordinate Telemetry Services**

### SignalR Event Subscriptions

#### SHOW_MESSAGE Event

```csharp
hubConnection.On<string, Dictionary<string, string?>?, bool>(SignalREvents.SHOW_MESSAGE, async (message, data) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to show.", message);
    
    if (await notification.IsNotificationAvailable())
    {
        // Show local notification using Notification API
        await notification.Show("Boilerplate SignalR", new()
        {
            Icon = "/images/icons/bit-icon-512.png",
            Body = message,
            Data = data
        });
    }
    else
    {
        if (data is not null) return false; // Snack bar doesn't support payload data
        SnackBarService.Show("Boilerplate", message);
    }

    return true; // Message shown successfully
});
```

This event handler:
- Receives a message from the server
- Shows it as a **native browser notification** if available
- Falls back to **BitSnackBar** if notifications aren't available
- Returns `true` if the message was shown successfully

**Server-Side Usage** (from `UserController.cs`):

```csharp
if (userSession.SignalRConnectionId != null)
{
    await appHubContext.Clients.Client(userSession.SignalRConnectionId)
        .SendAsync(SignalREvents.SHOW_MESSAGE, 
                   (string)Localizer[nameof(AppStrings.TestNotificationMessage2)], 
                   null, 
                   cancellationToken);
}
```

#### PUBLISH_MESSAGE Event

```csharp
hubConnection.On<string, object?>(SignalREvents.PUBLISH_MESSAGE, async (message, payload) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
    PubSubService.Publish(message, payload);
});
```

This bridges **SignalR** and **PubSubService**, allowing server-side code to publish messages that client-side components can subscribe to.

#### EXCEPTION_THROWN Event

```csharp
hubConnection.On<AppProblemDetails>(SignalREvents.EXCEPTION_THROWN, async (appProblemDetails) =>
{
    ExceptionHandler.Handle(appProblemDetails, displayKind: ExceptionDisplayKind.NonInterrupting);
});
```

This allows the server to push exceptions to the client for display (e.g., showing an error that occurred in a background job).

#### UPLOAD_DIAGNOSTIC_LOGGER_STORE Method

```csharp
hubConnection.On(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, async () =>
{
    return DiagnosticLogger.Store.ToArray();
});
```

This allows the server to **invoke a method on the client** and receive the client's diagnostic logs in response.

### Authentication State Propagation

The `PropagateAuthState` method is called when:
1. The application first starts
2. The user signs in or out

```csharp
public async Task PropagateAuthState(bool firstRun, Task<AuthenticationState> task)
{
    var user = (await task).User;
    var isAuthenticated = user.IsAuthenticated();
    var userId = isAuthenticated ? user.GetUserId() : (Guid?)null;
    
    if (lastPropagatedUserId == userId)
        return;
        
    TelemetryContext.UserId = userId;
    TelemetryContext.UserSessionId = isAuthenticated ? user.GetSessionId() : null;

    // Update App Insights
    if (isAuthenticated)
    {
        _ = appInsights.SetAuthenticatedUserContext(user.GetUserId().ToString());
    }
    else
    {
        _ = appInsights.ClearAuthenticatedUserContext();
    }

    // Start SignalR connection
    await EnsureSignalRStarted();

    // Subscribe to push notifications
    await pushNotificationService.Subscribe(CurrentCancellationToken);

    // Update user session info
    if (isAuthenticated)
    {
        await UpdateUserSession();
    }

    lastPropagatedUserId = userId;
}
```

This method ensures all services are updated when authentication state changes.

---

## 4. Push Notifications

### Push Notification Architecture

The Boilerplate project supports **push notifications** across all platforms:

- **Web (Browser)**: Web Push API with VAPID
- **Android**: Firebase Cloud Messaging (FCM)
- **iOS**: Apple Push Notification Service (APNS)
- **Windows**: Windows Notification Service (WNS)
- **macOS**: Apple Push Notification Service (APNS)

### Push Notification Subscription

**Client-Side Interface**: [`src/Client/Boilerplate.Client.Core/Services/Contracts/IPushNotificationService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IPushNotificationService.cs)

**Base Implementation**: [`src/Client/Boilerplate.Client.Core/Services/PushNotificationServiceBase.cs`](/src/Client/Boilerplate.Client.Core/Services/PushNotificationServiceBase.cs)

```csharp
public async Task Subscribe(CancellationToken cancellationToken)
{
    if (await IsAvailable(cancellationToken) is false)
    {
        Logger.LogWarning("Notifications are not supported/allowed on this platform/device.");
        return;
    }

    var subscription = await GetSubscription(cancellationToken);

    if (subscription is null)
        return;

    await pushNotificationController.Subscribe(subscription, cancellationToken);
}
```

Each platform has its own implementation:
- `WebPushNotificationService.cs`
- `AndroidPushNotificationService.cs`
- `iOSPushNotificationService.cs`
- `WindowsPushNotificationService.cs`
- `MacCatalystPushNotificationService.cs`

### Server-Side Push Notification Service

**Location**: [`src/Server/Boilerplate.Server.Api/Services/PushNotificationService.cs`](/src/Server/Boilerplate.Server.Api/Services/PushNotificationService.cs)

#### Subscribe Method

```csharp
public async Task Subscribe([Required] PushNotificationSubscriptionDto dto, CancellationToken cancellationToken)
{
    List<string> tags = [CultureInfo.CurrentUICulture.Name];

    var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() 
        ? httpContextAccessor.HttpContext.User.GetSessionId() 
        : (Guid?)null;

    var subscription = await dbContext.PushNotificationSubscriptions
        .WhereIf(userSessionId is null, s => s.DeviceId == dto.DeviceId)
        .WhereIf(userSessionId is not null, s => s.UserSessionId == userSessionId || s.DeviceId == dto.DeviceId)
        .FirstOrDefaultAsync(cancellationToken) ??
        (await dbContext.PushNotificationSubscriptions.AddAsync(new()
        {
            DeviceId = dto.DeviceId,
            Platform = dto.Platform
        }, cancellationToken)).Entity;

    dto.Patch(subscription);

    subscription.Tags = [.. tags];
    subscription.UserSessionId = userSessionId;
    subscription.RenewedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    subscription.ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds();

    if (subscription.Platform is "browser")
    {
        subscription.PushChannel = VapidSubscription.FromParameters(subscription.Endpoint, subscription.P256dh, subscription.Auth).ToAdsPushToken();
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}
```

**Key Features**:
- Associates push subscriptions with user sessions
- Supports both authenticated and anonymous subscriptions
- Uses **tags** for targeted notifications (e.g., by culture)
- Automatically renews and expires subscriptions

#### RequestPush Method

```csharp
public async Task RequestPush(
    string? title = null, 
    string? message = null,
    string? action = null,
    string? pageUrl = null,
    bool userRelatedPush = false,
    Expression<Func<PushNotificationSubscription, bool>>? customSubscriptionFilter = null,
    CancellationToken cancellationToken = default)
{
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    var query = dbContext.PushNotificationSubscriptions
        .Where(sub => sub.ExpirationTime > now)
        .Where(sub => sub.UserSessionId == null || sub.UserSession!.NotificationStatus == UserSessionNotificationStatus.Allowed)
        .WhereIf(customSubscriptionFilter is not null, customSubscriptionFilter!)
        .WhereIf(userRelatedPush is true, sub => (now - sub.RenewedOn) < serverApiSettings.Identity.RefreshTokenExpiration.TotalSeconds);

    if (customSubscriptionFilter is null)
    {
        query = query.OrderBy(_ => EF.Functions.Random()).Take(100);
    }

    var pushNotificationSubscriptionIds = await query.Select(pns => pns.Id).ToArrayAsync(cancellationToken);

    backgroundJobClient.Enqueue<PushNotificationJobRunner>(runner => runner.RequestPush(pushNotificationSubscriptionIds, title, message, action, pageUrl, userRelatedPush, default));
}
```

**Key Features**:
- Filters expired subscriptions
- Respects user notification preferences
- Supports custom filtering for targeted notifications
- Uses **Hangfire** background jobs for reliable delivery
- Prevents sending sensitive notifications to inactive devices

**Example Usage** (from `UserController.cs`):

```csharp
await pushNotificationService.RequestPush(
    message: Localizer[nameof(AppStrings.TestNotificationMessage1)], 
    userRelatedPush: true, 
    customSubscriptionFilter: us => us.UserSessionId == userSessionId, 
    cancellationToken: cancellationToken);
```

This sends a push notification to a specific user session.

### Push Notification vs SignalR vs Local Notification

The project uses **three types of notifications**:

1. **SignalR Messages**: Real-time messages to **connected** clients
   - Instant delivery
   - Only works if the app is open and connected
   - Example: `SHOW_MESSAGE` event

2. **Push Notifications**: Messages to devices even when the app is closed
   - Delivered via platform-specific services (FCM, APNS, WNS)
   - Works even if the app is not running
   - Example: `RequestPush` method

3. **Local Notifications**: Browser Notification API
   - Displayed locally by the browser
   - Doesn't require server or push service
   - Example: `notification.Show()` in `AppClientCoordinator.cs`

**Best Practice**: The application uses a **smart fallback strategy**:

```csharp
if (await notification.IsNotificationAvailable())
{
    // Show local notification
    await notification.Show("Boilerplate SignalR", new()
    {
        Icon = "/images/icons/bit-icon-512.png",
        Body = message,
        Data = data
    });
}
else
{
    // Fallback to snack bar
    SnackBarService.Show("Boilerplate", message);
}
```

---

## 5. Bit.Butil.Notification - Browser Notification API

The project uses **Bit.Butil.Notification** to access the browser's native Notification API.

**Extension Helper**: [`src/Client/Boilerplate.Client.Core/Extensions/NotificationExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/NotificationExtensions.cs)

```csharp
public static async Task<bool> IsNotificationAvailable(this Notification notification)
{
    var isPresent = await notification.IsSupported();
    if (isPresent)
    {
        if (await notification.GetPermission() is NotificationPermission.Granted)
            return true;
    }
    return false;
}
```

### Available Methods

- **`IsSupported()`**: Checks if the browser supports notifications
- **`GetPermission()`**: Gets current permission status (`Granted`, `Denied`, `Default`)
- **`RequestPermission()`**: Requests notification permission from the user
- **`Show(string title, NotificationOptions options)`**: Displays a native notification

### Usage Example

```csharp
[AutoInject] private Notification notification = default!;

private async Task ShowNotification()
{
    // Request permission first
    var permission = await notification.RequestPermission();
    
    if (permission is NotificationPermission.Granted)
    {
        // Show the notification
        await notification.Show("My App", new NotificationOptions
        {
            Body = "This is a notification message",
            Icon = "/images/icon.png",
            Data = new Dictionary<string, string> { { "pageUrl", "/dashboard" } }
        });
    }
}
```

**Important Notes**:
- Native notifications work even when the tab is not active
- They appear as system notifications (not inside the browser window)
- Users must grant permission before notifications can be shown
- The `Data` property can contain custom data for handling clicks

---

## 6. Session Revocation Example

Let's walk through a complete example of how session revocation works using all three messaging systems together.

### Scenario: Admin Revokes a User Session

**Step 1**: Admin calls the revoke session API

**Server-Side** (from `UserController.cs`):

```csharp
[HttpDelete("{userSessionId}")]
public async Task RevokeSession(Guid userSessionId, CancellationToken cancellationToken)
{
    var currentUserSessionId = User.GetSessionId();
    var userId = User.GetUserId();
    
    var userSession = await DbContext.UserSessions
        .FirstOrDefaultAsync(us => us.Id == userSessionId && us.UserId == userId, cancellationToken) 
        ?? throw new ResourceNotFoundException();

    DbContext.Remove(userSession);
    await DbContext.SaveChangesAsync(cancellationToken);

    List<Task> sendMessagesTasks = [];

    // Send SignalR message if connected
    if (string.IsNullOrEmpty(userSession.SignalRConnectionId) is false)
    {
        var message = Localizer[nameof(AppStrings.YouHaveBeenSignedOut)];
        
        sendMessagesTasks.Add(appHubContext.Clients.Client(userSession.SignalRConnectionId)
            .SendAsync(SignalREvents.SHOW_MESSAGE, message, null, cancellationToken));
    }

    // Send push notification if not connected
    if (userSession.Id != currentUserSessionId)
    {
        var message = Localizer[nameof(AppStrings.YourSessionHasExpired)];
        
        sendMessagesTasks.Add(pushNotificationService.RequestPush(
            message: message, 
            userRelatedPush: true, 
            customSubscriptionFilter: us => us.UserSession!.UserId == userId && us.UserSessionId != currentUserSessionId, 
            cancellationToken: cancellationToken));
    }

    await Task.WhenAll(sendMessagesTasks);
}
```

**Step 2**: Client receives the SignalR message

**Client-Side** (from `AppClientCoordinator.cs`):

```csharp
hubConnection.On<string, Dictionary<string, string?>?, bool>(SignalREvents.SHOW_MESSAGE, async (message, data) =>
{
    if (await notification.IsNotificationAvailable())
    {
        await notification.Show("Boilerplate SignalR", new()
        {
            Icon = "/images/icons/bit-icon-512.png",
            Body = message,
            Data = data
        });
    }
    else
    {
        SnackBarService.Show("Boilerplate", message);
    }
    return true;
});
```

**Step 3**: Client subscribes to SESSION_REVOKED

**Client-Side** (from `AuthManager.cs`):

```csharp
unsubscribe = pubSubService.Subscribe(SharedPubSubMessages.SESSION_REVOKED, _ => SignOut(default));
```

This ensures the user is automatically signed out and redirected to the sign-in page.

---

## 7. Dashboard Data Changed Example

Another common scenario is notifying all authenticated clients when data changes.

### Scenario: Product is Created/Updated/Deleted

**Server-Side** (from `ProductController.cs`):

```csharp
private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
{
    // Notify all authenticated clients
    await appHubContext.Clients.Group("AuthenticatedClients")
        .SendAsync(SignalREvents.PUBLISH_MESSAGE, 
                   SharedPubSubMessages.DASHBOARD_DATA_CHANGED, 
                   null, 
                   cancellationToken);
}
```

This is called after creating, updating, or deleting a product.

**Client-Side** (any component can subscribe):

```csharp
unsubscribe = PubSubService.Subscribe(SharedPubSubMessages.DASHBOARD_DATA_CHANGED, async (_) =>
{
    // Refresh dashboard data
    await LoadDashboardData();
    await InvokeAsync(StateHasChanged);
});
```

Components subscribe to `DASHBOARD_DATA_CHANGED` and automatically refresh when notified.

---

## 8. Best Practices

### ✅ DO

1. **Use SignalR for Real-Time Updates**
   - When clients are connected and need instant updates
   - For collaborative features where multiple users see changes immediately

2. **Use Push Notifications for Important Alerts**
   - When the app might not be open
   - For time-sensitive information (OTP codes, urgent alerts)

3. **Use PubSubService for Loose Coupling**
   - To decouple components from each other
   - For cross-component communication

4. **Respect User Notification Preferences**
   - Always check `UserSession.NotificationStatus`
   - Provide UI for users to manage notification preferences

5. **Handle Connection State Changes**
   - Subscribe to `IS_ONLINE_CHANGED` to detect connectivity issues
   - Implement retry logic for failed operations

6. **Use Persistent Messages When Appropriate**
   - For critical messages that must be delivered even if the subscriber isn't ready yet

### ❌ DON'T

1. **Don't Send Sensitive Data in Push Notifications**
   - Push notifications can be intercepted
   - Use generic messages and let the app fetch details

2. **Don't Spam Users with Notifications**
   - Respect rate limits
   - Filter subscriptions appropriately

3. **Don't Block UI on Notification Operations**
   - Use background jobs for sending notifications
   - Make subscription operations async

4. **Don't Forget to Clean Up Subscriptions**
   - Always unsubscribe in `Dispose` methods
   - Use the returned `Action` from `Subscribe()`

5. **Don't Send User-Related Push to Inactive Devices**
   - Check `RenewedOn` timestamp
   - Use `userRelatedPush: true` parameter

---

## 9. Diagnostic Features

### Retrieving User Session Logs

Admins and support staff can retrieve diagnostic logs from active user sessions:

**Server-Side** (from `AppHub.cs`):

```csharp
[Authorize(Policy = AppFeatures.System.ManageLogs)]
public async Task<DiagnosticLogDto[]> GetUserSessionLogs(Guid userSessionId, [FromServices] AppDbContext dbContext)
{
    var userSessionSignalRConnectionId = await dbContext.UserSessions
        .Where(us => us.Id == userSessionId)
        .Select(us => us.SignalRConnectionId)
        .FirstOrDefaultAsync(Context.ConnectionAborted);

    if (string.IsNullOrEmpty(userSessionSignalRConnectionId))
        return [];

    return await Clients.Client(userSessionSignalRConnectionId)
        .InvokeAsync<DiagnosticLogDto[]>(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, Context.ConnectionAborted);
}
```

**Client-Side** (from `UsersPage.razor.cs`):

```csharp
var logs = await hubConnection.InvokeAsync<DiagnosticLogDto[]>("GetUserSessionLogs", userSessionId);
```

This enables **live troubleshooting** by viewing logs from a user's active session.

### Testing Notifications

**From `UserController.cs` - ToggleNotification method**:

```csharp
if (userSession.NotificationStatus is UserSessionNotificationStatus.Allowed)
{
    // Send test push notification
    await pushNotificationService.RequestPush(
        message: Localizer[nameof(AppStrings.TestNotificationMessage1)], 
        userRelatedPush: true, 
        customSubscriptionFilter: us => us.UserSessionId == userSessionId, 
        cancellationToken: cancellationToken);
    
    // Send test SignalR message
    if (userSession.SignalRConnectionId != null)
    {
        await appHubContext.Clients.Client(userSession.SignalRConnectionId)
            .SendAsync(SignalREvents.SHOW_MESSAGE, 
                       (string)Localizer[nameof(AppStrings.TestNotificationMessage2)], 
                       null, 
                       cancellationToken);
    }
}
```

This allows users to test both push notifications and SignalR messages.

---

## 10. Configuration

### SignalR Configuration

**Server-Side** (`appsettings.json`):

```json
{
  "Azure": {
    "SignalR": {
      "ConnectionString": "..."
    }
  }
}
```

If Azure SignalR connection string is provided, the application uses Azure SignalR Service for scalability.

### Push Notification Configuration

**VAPID Keys** (for web push):

```json
{
  "PushNotificationService": {
    "VapidPublicKey": "...",
    "VapidPrivateKey": "..."
  }
}
```

**Platform-Specific Configuration**:
- **Android**: Firebase Cloud Messaging (FCM) credentials
- **iOS**: Apple Push Notification Service (APNS) certificates
- **Windows**: Windows Notification Service (WNS) credentials

---

## 11. Common Patterns

### Pattern 1: Notify All Users of Data Change

```csharp
// Server
await appHubContext.Clients.Group("AuthenticatedClients")
    .SendAsync(SignalREvents.PUBLISH_MESSAGE, "DATA_CHANGED", null, cancellationToken);

// Client
PubSubService.Subscribe("DATA_CHANGED", async (_) => await RefreshData());
```

### Pattern 2: Notify Specific User

```csharp
// Server
var userSessions = await DbContext.UserSessions
    .Where(us => us.UserId == targetUserId)
    .ToListAsync(cancellationToken);

foreach (var session in userSessions.Where(s => s.SignalRConnectionId != null))
{
    await appHubContext.Clients.Client(session.SignalRConnectionId!)
        .SendAsync(SignalREvents.SHOW_MESSAGE, "Your message", null, cancellationToken);
}
```

### Pattern 3: Notify by Culture

```csharp
// Server
await pushNotificationService.RequestPush(
    message: "Localized message",
    customSubscriptionFilter: sub => sub.Tags.Contains("en-US"),
    cancellationToken: cancellationToken);
```

### Pattern 4: Cross-Platform Communication

```csharp
// MAUI XAML Page
pubSubService.Publish(ClientPubSubMessages.NAVIGATE_TO, "/dashboard");

// Blazor Component
PubSubService.Subscribe(ClientPubSubMessages.NAVIGATE_TO, async (uri) =>
{
    NavigationManager.NavigateTo(uri?.ToString()!);
});
```

---

## Summary

The Boilerplate project's messaging system combines three powerful technologies:

1. **SignalR**: Real-time bi-directional communication for connected clients
2. **Push Notifications**: Platform-specific notifications for offline scenarios
3. **PubSubService**: Decoupled messaging for loose coupling and cross-component communication

Together, they provide:
- ✅ Real-time updates
- ✅ Offline notification delivery
- ✅ Cross-component communication
- ✅ Authentication state propagation
- ✅ Diagnostic capabilities
- ✅ Flexible targeting (all users, specific users, groups, cultures)

This architecture ensures your application can communicate effectively across all platforms and scenarios.

---
