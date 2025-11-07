# Stage 22: Messaging

Welcome to Stage 22! In this stage, you will learn about the comprehensive messaging architecture built into the project, which supports both **real-time bi-directional communication via SignalR** and **push notifications across all platforms** (web, mobile, and desktop).

## Overview

The project provides a complete messaging infrastructure that includes:

1. **SignalR Real-Time Communication**: Bi-directional communication between server and clients for instant updates
2. **Push Notifications**: Cross-platform push notification support for background messaging
3. **Local Notifications**: In-app notifications displayed within the application
4. **Pub/Sub Messaging**: Internal client-side event bus for component communication

---

## 1. SignalR Real-Time Communication

SignalR enables real-time, bi-directional communication between the server and connected clients.

### Server-Side: AppHub

**Location**: [`/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs)

The `AppHub` class is the central hub for SignalR communication:

```csharp
[AllowAnonymous]
public partial class AppHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.GetHttpContext()?.ContainsExpiredAccessToken() is true)
            throw new HubException(nameof(AppStrings.UnauthorizedException));

        await ChangeAuthenticationStateImplementation(Context.User);
        await base.OnConnectedAsync();
    }

    public Task ChangeAuthenticationState(string? accessToken)
    {
        // Updates authentication state when user signs in/out
        // Called by AppClientCoordinator.cs
    }
}
```

#### SignalR Messaging Capabilities

The hub supports several advanced scenarios:

1. **`Clients.All()`**: Broadcast to all connected clients (authenticated or not)
2. **`Clients.User(userId)`**: Send to all tabs/apps of a specific user
3. **`Clients.Group("AuthenticatedClients")`**: Send to all authenticated sessions
4. **`Clients.Client(signalRConnectionId)`**: Target specific session by connection ID

#### Real-World Example: Session Revocation

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.cs)

When an admin revokes a user session:

```csharp
[HttpDelete]
public async Task RevokeSession(Guid userSessionId, CancellationToken cancellationToken)
{
    var userSession = await dbContext.UserSessions
        .FirstOrDefaultAsync(us => us.Id == userSessionId, cancellationToken);

    if (userSession is null)
        throw new ResourceNotFoundException();

    if (string.IsNullOrEmpty(userSession.SignalRConnectionId) is false)
    {
        await appHubContext.Clients
            .Client(userSession.SignalRConnectionId)
            .SendAsync(SignalREvents.PUBLISH_MESSAGE, 
                       SharedPubSubMessages.SESSION_REVOKED, 
                       null, 
                       cancellationToken);
    }

    dbContext.Remove(userSession);
    await dbContext.SaveChangesAsync(cancellationToken);
}
```

This sends a SignalR message to the specific browser tab or app, which then:
- Clears access/refresh tokens from storage
- Navigates to the sign-in page automatically

### Client-Side: HubConnection & AppClientCoordinator

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs`](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs)

The `AppClientCoordinator` manages the SignalR connection lifecycle:

```csharp
public partial class AppClientCoordinator : AppComponentBase
{
    [AutoInject] private HubConnection hubConnection = default!;

    private async Task EnsureSignalRStarted()
    {
        if (hubConnection.State is not HubConnectionState.Connected or HubConnectionState.Connecting)
        {
            await hubConnection.StartAsync(CurrentCancellationToken);
            await HubConnectionConnected(null);
        }
        else
        {
            await hubConnection.InvokeAsync("ChangeAuthenticationState", 
                await AuthTokenProvider.GetAccessToken(), 
                CurrentCancellationToken);
        }
    }
}
```

#### SignalR Event Subscriptions

The client subscribes to three main SignalR events:

**1. SHOW_MESSAGE**: Display notifications to users

```csharp
hubConnection.On<string, Dictionary<string, string?>?, bool>(
    SignalREvents.SHOW_MESSAGE, 
    async (message, data) =>
    {
        if (await notification.IsNotificationAvailable())
        {
            // Show local notification (not push notification)
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
        return true; // Indicates message was shown successfully
    });
```

**2. PUBLISH_MESSAGE**: Broadcast messages internally via PubSub

```csharp
hubConnection.On<string, object?>(
    SignalREvents.PUBLISH_MESSAGE, 
    async (message, payload) =>
    {
        PubSubService.Publish(message, payload);
    });
```

**3. EXCEPTION_THROWN**: Handle server-side exceptions

```csharp
hubConnection.On<AppProblemDetails>(
    SignalREvents.EXCEPTION_THROWN, 
    async (appProblemDetails) =>
    {
        ExceptionHandler.Handle(appProblemDetails, 
            displayKind: ExceptionDisplayKind.NonInterrupting);
    });
```

### SignalR Message Keys

**Location**: [`/src/Shared/Services/SharedPubSubMessages.cs`](/src/Shared/Services/SharedPubSubMessages.cs)

The project defines message keys shared between server and client:

```csharp
// Events sent from server to client
public static partial class SignalREvents
{
    public const string PUBLISH_MESSAGE = nameof(PUBLISH_MESSAGE);
    public const string SHOW_MESSAGE = nameof(SHOW_MESSAGE);
    public const string EXCEPTION_THROWN = nameof(EXCEPTION_THROWN);
}

// Methods called on the hub
public static partial class SignalRMethods
{
    public const string UPLOAD_DIAGNOSTIC_LOGGER_STORE = 
        nameof(UPLOAD_DIAGNOSTIC_LOGGER_STORE);
}

// Pub/Sub message keys
public partial class SharedPubSubMessages
{
    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
}
```

### SignalR Configuration

**Server Configuration**: [`/src/Server/Boilerplate.Server.Api/Program.Services.cs`](/src/Server/Boilerplate.Server.Api/Program.Services.cs)

```csharp
var signalRBuilder = services.AddSignalR(options =>
{
    options.EnableDetailedErrors = serverApiSettings.IsTestOrDev();
});

// Optional: Azure SignalR Service for scaling across multiple servers
if (string.IsNullOrEmpty(serverApiSettings.Azure.SignalRConnectionString) is false)
{
    signalRBuilder.AddAzureSignalR(serverApiSettings.Azure.SignalRConnectionString);
}
```

**Endpoint Mapping**: [`/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs`](/src/Server/Boilerplate.Server.Web/Program.Middlewares.cs)

```csharp
app.MapHub<Api.SignalR.AppHub>("/app-hub", options => 
{
    options.AllowStatefulReconnects = true; // ASP.NET Core 8+ feature
});
```

**Client Configuration**: Handled automatically by dependency injection, with the connection string configured in `appsettings.json`.

---

## 2. Push Notifications

Push notifications enable the server to send messages to users even when they're not actively using the application.

### Architecture Overview

The push notification system consists of:

1. **Client Services**: Platform-specific implementations for subscribing to push notifications
2. **Server Service**: `PushNotificationService` manages subscriptions and sends notifications
3. **Background Jobs**: `PushNotificationJobRunner` handles actual notification delivery
4. **DTO & Models**: Subscription data structures

### Push Notification DTO

**Location**: [`/src/Shared/Dtos/PushNotification/PushNotificationSubscriptionDto.cs`](/src/Shared/Dtos/PushNotification/PushNotificationSubscriptionDto.cs)

```csharp
[DtoResourceType(typeof(AppStrings))]
public partial class PushNotificationSubscriptionDto
{
    [Required]
    public string? DeviceId { get; set; }

    [Required]
    [AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    public string? PushChannel { get; set; }
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }
}
```

**Supported Platforms**:
- **`browser`**: Web Push API (Chrome, Edge, Firefox, Safari)
- **`fcmV1`**: Firebase Cloud Messaging (Android)
- **`apns`**: Apple Push Notification service (iOS, macOS)

### Client-Side: IPushNotificationService

**Base Interface**: [`/src/Client/Boilerplate.Client.Core/Services/Contracts/IPushNotificationService.cs`](/src/Client/Boilerplate.Client.Core/Services/Contracts/IPushNotificationService.cs)

```csharp
public interface IPushNotificationService
{
    string Token { get; set; }
    
    /// <summary>
    /// Supported by the OS/Platform and allowed by the user.
    /// </summary>
    Task<bool> IsAvailable(CancellationToken cancellationToken);
    
    Task RequestPermission(CancellationToken cancellationToken);
    Task<PushNotificationSubscriptionDto?> GetSubscription(CancellationToken cancellationToken);
    Task Subscribe(CancellationToken cancellationToken);
}
```

**Base Implementation**: [`/src/Client/Boilerplate.Client.Core/Services/PushNotificationServiceBase.cs`](/src/Client/Boilerplate.Client.Core/Services/PushNotificationServiceBase.cs)

```csharp
public abstract partial class PushNotificationServiceBase : IPushNotificationService
{
    [AutoInject] protected ILogger<PushNotificationServiceBase> Logger = default!;
    [AutoInject] protected IPushNotificationController pushNotificationController = default!;

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        if (await IsAvailable(cancellationToken) is false)
        {
            Logger.LogWarning("Notifications are not supported/allowed.");
            return;
        }

        var subscription = await GetSubscription(cancellationToken);
        if (subscription is null) return;

        await pushNotificationController.Subscribe(subscription, cancellationToken);
    }
}
```

### Platform-Specific Implementations

#### 1. Web (Browser) Push Notifications

**Location**: [`/src/Client/Boilerplate.Client.Web/Services/WebPushNotificationService.cs`](/src/Client/Boilerplate.Client.Web/Services/WebPushNotificationService.cs)

```csharp
public partial class WebPushNotificationService : PushNotificationServiceBase
{
    [AutoInject] private Notification notification = default!;
    [AutoInject] private readonly IJSRuntime jSRuntime = default!;
    [AutoInject] private readonly ClientWebSettings clientWebSettings = default!;

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(
        CancellationToken cancellationToken)
    {
        return await jSRuntime.GetPushNotificationSubscription(
            clientWebSettings.AdsPushVapid!.PublicKey);
    }

    public override async Task<bool> IsAvailable(CancellationToken cancellationToken) 
        => string.IsNullOrEmpty(clientWebSettings.AdsPushVapid?.PublicKey) is false 
            && await notification.IsNotificationAvailable();

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        if (await notification.IsSupported() is false) return;
        await notification.RequestPermission();
    }
}
```

**JavaScript Integration**: [`/src/Client/Boilerplate.Client.Core/Scripts/App.ts`](/src/Client/Boilerplate.Client.Core/Scripts/App.ts)

```typescript
public static async getPushNotificationSubscription(vapidPublicKey: string) {
    const registration = await navigator.serviceWorker.ready;
    if (!registration) return null;

    const pushManager = registration.pushManager;
    if (!pushManager) return null;

    let subscription = await pushManager.getSubscription();

    if (!subscription) {
        subscription = await pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: vapidPublicKey
        });
    }

    const pushChannel = subscription.toJSON();
    const p256dh = pushChannel.keys!['p256dh'];
    const auth = pushChannel.keys!['auth'];

    return {
        deviceId: `${p256dh}-${auth}`,
        platform: 'browser',
        p256dh: p256dh,
        auth: auth,
        endpoint: pushChannel.endpoint
    };
}
```

**Service Worker**: [`/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.published.js)

```javascript
self.addEventListener('push', function (event) {
    const eventData = event.data.json();
    
    self.registration.showNotification(eventData.title, {
        data: eventData.data,
        body: eventData.message,
        icon: '/images/icons/bit-icon-512.png'
    });
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const pageUrl = event.notification.data.pageUrl;
    
    if (pageUrl != null) {
        event.waitUntil(
            clients.matchAll({ type: 'window', includeUncontrolled: true })
                .then((clientList) => {
                    for (const client of clientList) {
                        if (!client.focus || !client.postMessage) continue;
                        // Navigate to the page
                        client.postMessage({ 
                            key: 'PUBLISH_MESSAGE', 
                            message: 'NAVIGATE_TO', 
                            payload: pageUrl 
                        });
                        return client.focus();
                    }
                    return clients.openWindow(pageUrl);
                })
        );
    }
});
```

#### 2. Android Push Notifications (Firebase Cloud Messaging)

**Location**: [`/src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/AndroidPushNotificationService.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/AndroidPushNotificationService.cs)

```csharp
public partial class AndroidPushNotificationService : PushNotificationServiceBase
{
    public override async Task<bool> IsAvailable(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            return LocalNotificationCenter.Current.IsSupported
                && await LocalNotificationCenter.Current.AreNotificationsEnabled();
        });
    }

    public override async Task RequestPermission(CancellationToken cancellationToken)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (LocalNotificationCenter.Current.IsSupported is false) return;
            await LocalNotificationCenter.Current.RequestNotificationPermission();
            Configure();
        });
    }

    public string GetDeviceId() 
        => Secure.GetString(Platform.AppContext.ContentResolver, Secure.AndroidId)!;

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(
        CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, cts.Token);

            // Wait for Firebase token
            while (string.IsNullOrEmpty(Token))
            {
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
        }
        catch (Exception exp)
        {
            Logger.LogError(exp, "Unable to resolve token for FCMv1.");
            return null;
        }

        return new PushNotificationSubscriptionDto
        {
            DeviceId = GetDeviceId(),
            Platform = "fcmV1",
            PushChannel = Token
        };
    }
}
```

**Firebase Messaging Service**: [`/src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/PushNotificationFirebaseMessagingService.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/Android/Services/PushNotificationFirebaseMessagingService.cs)

```csharp
[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public partial class PushNotificationFirebaseMessagingService : FirebaseMessagingService
{
    public override async void OnNewToken(string token)
    {
        PushNotificationService.Token = token;
        await PushNotificationService.Subscribe(default);
    }

    public override async void OnMessageReceived(RemoteMessage message)
    {
        var notification = message.GetNotification();
        var title = notification!.Title;
        var body = notification.Body;

        if (string.IsNullOrEmpty(title) is false)
        {
            await LocalNotificationCenter.Current.Show(new()
            {
                Title = title!,
                Description = body!,
                ReturningData = JsonSerializer.Serialize(
                    message.Data ?? new Dictionary<string, string> { })
            });
        }
    }
}
```

#### 3. iOS/macOS Push Notifications (APNS)

**Location**: [`/src/Client/Boilerplate.Client.Maui/Platforms/iOS/Services/iOSPushNotificationService.cs`](/src/Client/Boilerplate.Client.Maui/Platforms/iOS/Services/iOSPushNotificationService.cs)

```csharp
public partial class iOSPushNotificationService : PushNotificationServiceBase
{
    public override async Task<bool> IsAvailable(CancellationToken cancellationToken)
    {
        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            return LocalNotificationCenter.Current.IsSupported
                && await LocalNotificationCenter.Current.AreNotificationsEnabled();
        });
    }

    public string GetDeviceId() 
        => UIDevice.CurrentDevice.IdentifierForVendor!.ToString();

    public override async Task<PushNotificationSubscriptionDto?> GetSubscription(
        CancellationToken cancellationToken)
    {
        // Similar to Android, waits for APNS token
        // Returns subscription with platform = "apns"
    }

    public static async Task Configure()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            UIApplication.SharedApplication.RegisterForRemoteNotifications();
            UNUserNotificationCenter.Current.Delegate = 
                new AppUNUserNotificationCenterDelegate();
        });
    }
}
```

#### 4. Windows Push Notifications

**Location**: [`/src/Client/Boilerplate.Client.Windows/Services/WindowsPushNotificationService.cs`](/src/Client/Boilerplate.Client.Windows/Services/WindowsPushNotificationService.cs)

```csharp
public partial class WindowsPushNotificationService : PushNotificationServiceBase
{
    public override Task<PushNotificationSubscriptionDto?> GetSubscription(
        CancellationToken cancellationToken) 
        => Task.FromResult<PushNotificationSubscriptionDto?>(null);
}
```

**Note**: Windows push notifications are not yet fully implemented but can be added using Windows Notification Service (WNS).

### Server-Side: Push Notification Management

#### PushNotificationService

**Location**: [`/src/Server/Boilerplate.Server.Api/Services/PushNotificationService.cs`](/src/Server/Boilerplate.Server.Api/Services/PushNotificationService.cs)

```csharp
public partial class PushNotificationService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IBackgroundJobClient backgroundJobClient = default!;

    public async Task Subscribe([Required] PushNotificationSubscriptionDto dto, 
        CancellationToken cancellationToken)
    {
        // Tags can be used to send push notifications to specific groups
        List<string> tags = [
            CultureInfo.CurrentUICulture.Name // Send to users with specific culture
        ];

        var userSessionId = httpContextAccessor.HttpContext!.User.IsAuthenticated() 
            ? httpContextAccessor.HttpContext.User.GetSessionId() 
            : (Guid?)null;

        // Find or create subscription
        var subscription = await dbContext.PushNotificationSubscriptions
            .WhereIf(userSessionId is null, s => s.DeviceId == dto.DeviceId)
            .WhereIf(userSessionId is not null, 
                s => s.UserSessionId == userSessionId || s.DeviceId == dto.DeviceId)
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
        subscription.ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1)
            .ToUnixTimeSeconds();

        if (subscription.Platform is "browser")
        {
            subscription.PushChannel = VapidSubscription
                .FromParameters(subscription.Endpoint, 
                               subscription.P256dh, 
                               subscription.Auth)
                .ToAdsPushToken();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

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
            .Where(sub => sub.UserSessionId == null || 
                         sub.UserSession!.NotificationStatus == UserSessionNotificationStatus.Allowed)
            .WhereIf(customSubscriptionFilter is not null, customSubscriptionFilter!)
            .WhereIf(userRelatedPush is true, 
                sub => (now - sub.RenewedOn) < serverApiSettings.Identity
                    .RefreshTokenExpiration.TotalSeconds);

        if (customSubscriptionFilter is null)
        {
            query = query.OrderBy(_ => EF.Functions.Random()).Take(100);
        }

        var pushNotificationSubscriptionIds = await query
            .Select(pns => pns.Id)
            .ToArrayAsync(cancellationToken);

        // Enqueue background job to send notifications
        backgroundJobClient.Enqueue<PushNotificationJobRunner>(runner => 
            runner.RequestPush(pushNotificationSubscriptionIds, 
                              title, message, action, pageUrl, 
                              userRelatedPush, default));
    }
}
```

#### Key Concepts

**1. User-Related Push**:
```csharp
// userRelatedPush: If the BearerTokenExpiration is 14 days, it's not practical 
// to send push notifications with sensitive information, like an OTP code to a 
// device where the user hasn't used the app for over 14 days.
// This is because, even if the user opens the app, they will be automatically 
// signed out as their session has expired.
```

**2. Custom Subscription Filter**:
```csharp
// Example: Send to specific user
await pushNotificationService.RequestPush(
    message: message, 
    userRelatedPush: true, 
    customSubscriptionFilter: us => us.UserSession!.UserId == user.Id 
        && us.UserSessionId != currentUserSessionId, 
    cancellationToken: cancellationToken);
```

#### PushNotificationJobRunner

**Location**: [`/src/Server/Boilerplate.Server.Api/Services/Jobs/PushNotificationJobRunner.cs`](/src/Server/Boilerplate.Server.Api/Services/Jobs/PushNotificationJobRunner.cs)

```csharp
public partial class PushNotificationJobRunner
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IAdsPushSender adsPushSender = default!;
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    public async Task RequestPush(
        int[] pushNotificationSubscriptionIds,
        string? title = null,
        string? message = null,
        string? action = null,
        string? pageUrl = null,
        bool userRelatedPush = false,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await dbContext.PushNotificationSubscriptions
            .Where(pns => pushNotificationSubscriptionIds.Contains(pns.Id))
            .ToArrayAsync(cancellationToken);

        var payload = new AdsPushBasicSendPayload()
        {
            Title = AdsPushText.CreateUsingString(title ?? "Boilerplate push"),
            Detail = AdsPushText.CreateUsingString(message ?? string.Empty)
        };

        if (string.IsNullOrEmpty(action) is false)
        {
            payload.Parameters.Add("action", action);
        }
        if (string.IsNullOrEmpty(pageUrl) is false)
        {
            payload.Parameters.Add("pageUrl", pageUrl);
        }

        ConcurrentBag<Exception> exceptions = [];

        await Parallel.ForEachAsync(subscriptions, parallelOptions: new()
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = default
        }, async (subscription, _) =>
        {
            try
            {
                var target = subscription.Platform is "browser" 
                    ? AdsPushTarget.BrowserAndPwa
                    : subscription.Platform is "fcmV1" 
                        ? AdsPushTarget.Android
                        : subscription.Platform is "apns" 
                            ? AdsPushTarget.Ios
                            : throw new NotImplementedException();

                await adsPushSender.BasicSendAsync(
                    target, 
                    subscription.PushChannel, 
                    payload, 
                    default);
            }
            catch (Exception exp)
            {
                exceptions.Add(exp);
            }
        });

        if (exceptions.IsEmpty is false)
        {
            serverExceptionHandler.Handle(
                new AggregateException("Failed to send push notifications", exceptions)
                    .WithData(new() { { "UserRelatedPush", userRelatedPush } }));
        }
    }
}
```

**Why Use Background Jobs?**
- Push notifications are sent in parallel to multiple devices
- Failures on individual devices don't block the entire operation
- The job continues even if the original HTTP request is canceled or times out
- Hangfire ensures the job is retried if it fails

---

## 3. Local Notifications

Local notifications are displayed **within the application** when it's running, separate from push notifications.

### Plugin.LocalNotification

The project uses the **`Plugin.LocalNotification`** NuGet package for local notification support on mobile platforms:

```csharp
// Check if local notifications are available
if (LocalNotificationCenter.Current.IsSupported 
    && await LocalNotificationCenter.Current.AreNotificationsEnabled())
{
    // Show local notification
    await LocalNotificationCenter.Current.Show(new()
    {
        Title = "Local Notification",
        Description = "This is shown within the app",
        ReturningData = JsonSerializer.Serialize(additionalData)
    });
}
```

### Bit.Butil Notification (Web)

For web applications, the project uses **`Bit.Butil`**'s Notification API:

```csharp
[AutoInject] private Notification notification = default!;

// Check availability
if (await notification.IsNotificationAvailable())
{
    // Show browser notification
    await notification.Show("Title", new()
    {
        Icon = "/images/icons/bit-icon-512.png",
        Body = "Notification message",
        Data = additionalData
    });
}
```

---

## 4. Pub/Sub Messaging (Internal Client Communication)

The project includes an internal pub/sub messaging system for communication **between components on the client side**.

### IPubSubService

**Usage Example**:

```csharp
// Subscribe to a message
unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, 
    async (payload) =>
    {
        // Handle profile update
        await RefreshUserProfile();
    });

// Publish a message
PubSubService.Publish(ClientPubSubMessages.PROFILE_UPDATED, userDto);

// Unsubscribe when done
unsubscribe?.Invoke();
```

### Client-Only Pub/Sub Messages

**Location**: Search for `ClientPubSubMessages` in the codebase

```csharp
public partial class ClientPubSubMessages
{
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string IS_ONLINE_CHANGED = nameof(IS_ONLINE_CHANGED);
    public const string FORCE_UPDATE = nameof(FORCE_UPDATE);
}
```

---

## 5. Real-World Example: Multi-Channel Messaging

Let's examine how the project uses **both SignalR and Push Notifications** together.

**Scenario**: Admin sends an elevated access token to a user

**Location**: [`/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Identity/UserController.cs)

```csharp
[HttpPost]
public async Task SendElevatedAccessToken(CancellationToken cancellationToken)
{
    var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
    if (user is null) throw new ResourceNotFoundException();

    // Generate elevated access token
    var message = Localizer[nameof(AppStrings.ElevatedAccessTokenSent)];

    List<Task> sendMessagesTasks = [];

    // 1. Send via SignalR to all active sessions except current
    var userSessionIdsExceptCurrentUserSessionId = await dbContext.UserSessions
        .Where(us => us.UserId == user.Id && us.Id != currentUserSessionId)
        .Where(us => us.SignalRConnectionId != null)
        .Select(us => us.SignalRConnectionId!)
        .ToArrayAsync(cancellationToken);
        
    sendMessagesTasks.Add(
        appHubContext.Clients
            .Clients(userSessionIdsExceptCurrentUserSessionId)
            .SendAsync(SignalREvents.SHOW_MESSAGE, message, null, cancellationToken));

    // 2. Send push notification to user's other devices
    sendMessagesTasks.Add(
        pushNotificationService.RequestPush(
            message: message, 
            userRelatedPush: true, 
            customSubscriptionFilter: us => us.UserSession!.UserId == user.Id 
                && us.UserSessionId != currentUserSessionId, 
            cancellationToken: cancellationToken));

    await Task.WhenAll(sendMessagesTasks);
}
```

This ensures the user receives the message through:
- **SignalR**: If they have the app open in another tab/device
- **Push Notification**: If they're not actively using the app

---

## 6. Configuration

### Push Notification Configuration

**Server-Side Configuration**: [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
{
  "AdsPush": {
    "Vapid": {
      "PublicKey": "YOUR_VAPID_PUBLIC_KEY",
      "PrivateKey": "YOUR_VAPID_PRIVATE_KEY",
      "Subject": "mailto:support@bitplatform.dev"
    },
    "FcmV1": {
      "GoogleApplicationCredentials": "path/to/firebase-service-account.json"
    },
    "Apns": {
      "BundleId": "com.bitplatform.Boilerplate",
      "P8PrivateKey": "path/to/apns-key.p8",
      "P8PrivateKeyId": "YOUR_KEY_ID",
      "TeamId": "YOUR_TEAM_ID"
    }
  }
}
```

**Client-Side Configuration**: [`/src/Client/Boilerplate.Client.Web/appsettings.json`](/src/Client/Boilerplate.Client.Web/appsettings.json)

```json
{
  "AdsPushVapid": {
    "PublicKey": "YOUR_VAPID_PUBLIC_KEY"
  }
}
```

### SignalR Configuration

**Server-Side** (optional Azure SignalR Service): [`/src/Server/Boilerplate.Server.Api/appsettings.json`](/src/Server/Boilerplate.Server.Api/appsettings.json)

```json
{
  "Azure": {
    "SignalRConnectionString": "Endpoint=https://...;AccessKey=...;"
  }
}
```

---

## 7. Key Takeaways

### When to Use Each Messaging Type

| Messaging Type | Use Case | Example |
|----------------|----------|---------|
| **SignalR** | Real-time bi-directional communication for active sessions | Live chat, real-time dashboards, instant notifications |
| **Push Notifications** | Background messaging when app is closed or inactive | OTP codes, breaking news alerts, reminders |
| **Local Notifications** | In-app alerts when app is running | Download complete, task finished |
| **Pub/Sub** | Internal client-side component communication | Update UI after data change, coordinate between components |

### Best Practices

1. **Combine SignalR + Push Notifications**: Send via SignalR for active users and push notifications for inactive users
2. **Use Background Jobs for Push**: Never send push notifications directly in API calls
3. **Respect User Preferences**: Check `NotificationStatus` before sending user-related pushes
4. **Handle Token Expiration**: Don't send sensitive data to devices with expired sessions
5. **Graceful Degradation**: Check if notification APIs are available before using them
6. **Error Handling**: Push notification failures shouldn't crash your application

### Debugging Tips

1. **Check SignalR Connection State**: Use the diagnostic modal to view SignalR connection status
2. **View Push Subscriptions**: Query `PushNotificationSubscriptions` table to see registered devices
3. **Monitor Hangfire Dashboard**: Check background job execution for push notification delivery
4. **Test with Actual Devices**: Push notifications may not work in emulators/simulators
5. **Check Service Worker Registration**: For web push, ensure service worker is registered

---

## 8. Advanced Topics

### Diagnostic Logger Upload via SignalR

**Location**: [`/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs)

Support staff can retrieve diagnostic logs from user sessions:

```csharp
[Authorize(Policy = AppFeatures.System.ManageLogs)]
public async Task<DiagnosticLogDto[]> GetUserSessionLogs(
    Guid userSessionId, 
    [FromServices] AppDbContext dbContext)
{
    var userSessionSignalRConnectionId = await dbContext.UserSessions
        .Where(us => us.Id == userSessionId)
        .Select(us => us.SignalRConnectionId)
        .FirstOrDefaultAsync(Context.ConnectionAborted);

    if (string.IsNullOrEmpty(userSessionSignalRConnectionId))
        return [];

    return await Clients.Client(userSessionSignalRConnectionId)
        .InvokeAsync<DiagnosticLogDto[]>(
            SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, 
            Context.ConnectionAborted);
}
```

This allows support staff to view real-time logs from user devices for troubleshooting.

### AI Chatbot Streaming via SignalR

**Location**: [`/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.Chatbot.cs)

The project uses SignalR for streaming AI chatbot responses:

```csharp
public async IAsyncEnumerable<string> Chatbot(
    string prompt, 
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var response in chatService.GetStreamingResponse(prompt, cancellationToken))
    {
        yield return response;
    }
}
```

**Client-Side**:

```csharp
await foreach (var response in hubConnection.StreamAsync<string>(
    "Chatbot", prompt, CurrentCancellationToken))
{
    messageText += response;
    StateHasChanged();
}
```

---

## Summary

The project provides a **complete, production-ready messaging infrastructure** that includes:

✅ **SignalR** for real-time bi-directional communication  
✅ **Cross-platform push notifications** (Web, Android, iOS, macOS, Windows)  
✅ **Local notifications** for in-app alerts  
✅ **Pub/Sub messaging** for internal client communication  
✅ **Background job processing** for reliable message delivery  
✅ **Multi-channel messaging** (SignalR + Push) for maximum reach  
✅ **Diagnostic tools** for troubleshooting  
✅ **AI chatbot streaming** capabilities  

All of this works seamlessly across **Blazor Server**, **Blazor WebAssembly**, **.NET MAUI**, and **Blazor Hybrid** applications.

---
