# Stage 22: Messaging

Welcome to Stage 22! In this stage, you will learn about the comprehensive **messaging and real-time communication system** built into the Boilerplate project. This system provides a unified messaging architecture that enables communication across multiple channels and platforms.

---

## 1. AppMessages - The Centralized Messaging System

### Overview

At the heart of the Boilerplate messaging architecture is **AppMessages** - a centralized messaging system that provides a consistent way to communicate across different parts of your application, regardless of whether the communication happens:

- Between C# components on the client side
- From server to client through SignalR
- From JavaScript to C# code
- From web service workers to the C# code

**Location**: [`src/Shared/Services/SharedAppMessages.cs`](/src/Shared/Services/SharedAppMessages.cs)

**Location**: [`src/Shared/Services/ClientAppMessages.cs`](/src/Client/Boilerplate.Client.Core/Services/ClientAppMessages.cs)

### Message Structure

**SharedAppMessages** (Server ↔ Client):

```csharp
public partial class SharedAppMessages
{
    // Data change notifications
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    
    // Session management
    public const string SESSION_REVOKED = nameof(SESSION_REVOKED);
    
    // Profile updates
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    
    // Navigation and UI changes
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string CHANGE_CULTURE = nameof(CHANGE_CULTURE);
    public const string CHANGE_THEME = nameof(CHANGE_THEME);
    
    // ... and more
}
```

**ClientAppMessages** (Client-Only):

**Location**: [`src/Client/Boilerplate.Client.Core/Services/ClientAppMessages.cs`](/src/Client/Boilerplate.Client.Core/Services/ClientAppMessages.cs)

```csharp
public partial class ClientAppMessages : SharedAppMessages
{    
    // Theme and culture
    public const string THEME_CHANGED = nameof(THEME_CHANGED);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);
    
    // Diagnostics
    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);
    
    // ... and more
}
```

**Note**: `ClientAppMessages` inherits from `SharedAppMessages`, so client-side code has access to both shared and client-only messages.

---

## 2. Communication Channels

The Boilerplate project provides multiple communication channels that all work with the same centralized message system. Let's explore each channel and understand when to use them.

### Channel 1: PubSubService (Client-Side Component Communication)

**PubSubService** is the foundation for client-side messaging. It implements a publish/subscribe pattern for decoupled communication between components.

**Location**: [`src/Client/Boilerplate.Client.Core/Services/PubSubService.cs`](/src/Client/Boilerplate.Client.Core/Services/PubSubService.cs)

**When to use**:
- Communication between Blazor components
- Communication with non-Blazor components (e.g., MAUI XAML pages)
- Broadcasting UI state changes
- Triggering actions across unrelated components

**Publishing a message**:

```csharp
// From any component or service
PubSubService.Publish(ClientAppMessages.THEME_CHANGED, newTheme);
```

**Subscribing to messages**:

```csharp
// In component code
private Action? unsubscribe;

protected override void OnInitialized()
{
    unsubscribe = PubSubService.Subscribe(ClientAppMessages.THEME_CHANGED, async payload =>
    {
        currentTheme = (string)payload;
        await InvokeAsync(StateHasChanged);
    });
}

protected override void Dispose(bool disposing)
{
    unsubscribe?.Invoke();
    base.Dispose(disposing);
}
```

**Persistent Messages**:

If `persistent = true`, the message is stored and delivered to handlers that **subscribe after** the message was published:

```csharp
PubSubService.Publish(ClientAppMessages.PROFILE_UPDATED, user, persistent: true);
```

### Channel 2: SignalR (Server-to-Client Real-Time Communication)

**SignalR** enables the server to send messages to clients in real-time. In the Boilerplate project, SignalR messages are automatically bridged to PubSubService, creating a seamless experience.

**Server-Side Hub**: [`src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs)

**When to use**:
- Notifying clients of data changes
- Broadcasting updates to all users or specific users
- Pushing real-time notifications
- Synchronizing state across multiple devices

**Publishing from server to all authenticated clients**:

```csharp
// Notify all authenticated users that dashboard data changed
await appHubContext.Clients.Group("AuthenticatedClients")
    .Publish(SharedAppMessages.DASHBOARD_DATA_CHANGED, null, cancellationToken);
```

**Publishing from server to specific user's all devices**:

```csharp
// When user updates profile on one device, notify all their other devices
await appHubContext.Clients.User(userId.ToString())
    .Publish(SharedAppMessages.PROFILE_UPDATED, userDto, cancellationToken);
```

**Client-Side Reception**:

On the client side, SignalR messages are automatically bridged to PubSubService (see `AppClientCoordinator.cs`). This means any component can subscribe to these messages using PubSubService:

```csharp
// Component subscribes to server-sent messages the same way as client-only messages
unsubscribe = PubSubService.Subscribe(SharedAppMessages.DASHBOARD_DATA_CHANGED, async (_) =>
{
    await LoadDashboardData();
    await InvokeAsync(StateHasChanged);
});
```

### Channel 3: AppJsBridge (JavaScript-to-C# Communication)

**AppJsBridge** enables JavaScript code to publish messages to the C# PubSubService.

**Location**: [`src/Client/Boilerplate.Client.Core/Components/Layout/AppJsBridge.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/AppJsBridge.razor.cs)

**When to use**:
- Integrating third-party JavaScript libraries
- Handling browser events that need C# processing
- Calling C# code from JavaScript

**Publishing from JavaScript**:

```javascript
// From any JavaScript code
App.publishMessage('CUSTOM_EVENT', { data: 'some data' });

// Show diagnostic modal
App.showDiagnostic(); // Publishes SHOW_DIAGNOSTIC_MODAL message
```

**How it works**:

```csharp
// AppJsBridge.razor.cs
[JSInvokable(nameof(PublishMessage))]
public async Task PublishMessage(string message, string? payload)
{
    // JavaScript messages are published to PubSubService
    PubSubService.Publish(message, payload);
}
```

### Channel 4: window.postMessage (Cross-Context JavaScript Communication)

The `window.postMessage` API allows communication between different JavaScript contexts (e.g., iframes, service workers). The Boilerplate project bridges this to PubSubService.

**Location**: [`src/Client/Boilerplate.Client.Core/Scripts/events.ts`](/src/Client/Boilerplate.Client.Core/Scripts/events.ts)

**When to use**:
- Communication from iframes
- Integration with third-party scripts
- Cross-origin messaging

**Publishing via window.postMessage**:

```javascript
// From any JavaScript context (including iframes)
window.postMessage({ 
    key: 'PUBLISH_MESSAGE', 
    message: 'CUSTOM_EVENT', 
    payload: { data: 'value' } 
}, '*');
```

**How it works**:

```typescript
// events.ts
window.addEventListener('message', handleMessage);

function handleMessage(e: MessageEvent) {
    if (e.data?.key === 'PUBLISH_MESSAGE') {
        // Bridge to C# PubSubService via AppJsBridge
        App.publishMessage(e.data?.message, e.data?.payload);
    }
}
```

### Channel 5: Service Worker (Background Message Handling)

Service workers can communicate with the Blazor application using the same messaging system. This is particularly useful for handling push notification clicks.

**Location**: [`src/Client/Boilerplate.Client.Web/wwwroot/service-worker.js`](/src/Client/Boilerplate.Client.Web/wwwroot/service-worker.js)

**When to use**:
- Handling push notification clicks
- Background synchronization
- Cache management notifications

**Publishing from service worker**:

```javascript
// service-worker.js
self.addEventListener('notificationclick', (event) => {
    const pageUrl = event.notification.data?.pageUrl;
    // ...
    // Send NAVIGATE_TO message to open specific page in app
    client.postMessage({ 
        key: 'PUBLISH_MESSAGE', 
        message: 'NAVIGATE_TO', 
        payload: pageUrl 
    });
    return client.focus();
});
```

**How it works**:

```typescript
// events.ts - Listens for service worker messages
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('message', handleMessage);
}

function handleMessage(e: MessageEvent) {
    if (e.data?.key === 'PUBLISH_MESSAGE') {
        // Bridge to C# PubSubService
        App.publishMessage(e.data?.message, e.data?.payload);
    }
}
```

---

## 3. SignalR Details: SendAsync vs InvokeAsync

Understanding when to use `SendAsync` (or its wrapper `Publish`) vs `InvokeAsync` is crucial for reliable server-to-client communication.

### When to Use InvokeAsync

Use `InvokeAsync` when:
- Sending a message to a **specific SignalR connection ID**
- It's **important to know** if the message arrived on the client side
- You need confirmation that the operation completed successfully
- You're waiting for a response from the client

**Example**:

```csharp
// Server needs to know if the message was successfully shown to the user
var messageShown = await appHubContext.Clients.Client(userSession.SignalRConnectionId)
    .InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, message, null, cancellationToken);

if (messageShown)
{
    // Message was successfully shown to the user
}
```

**Important**: For `InvokeAsync` to work, the client-side `hubConnection.On` listener must be registered in **`AppClientCoordinator.cs`** in the `SubscribeToSignalRSharedAppMessages` method, and it **must return a value** even a simple `true` const:

```csharp
// Client-side in AppClientCoordinator.cs
hubConnection.On<string, Dictionary<string, string?>?, bool>(SharedAppMessages.SHOW_MESSAGE, async (message, data) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to show.", message);
    
    await ShowNotificationOrSnack(message, data);
    
    return true;
});
```

### When to Use SendAsync (or Publish)

Use `SendAsync` or the `Publish` extension method when:
- Broadcasting to multiple clients (e.g., `Clients.All()`, `Clients.Group()`, `Clients.User()`)
- Fire-and-forget messaging is acceptable
- You don't need confirmation of delivery

**Example**:

```csharp
// Notify all authenticated clients - no need to wait for confirmation
await appHubContext.Clients.Group("AuthenticatedClients").SendAsync(SharedAppMessages.PUBLISH_MESSAGE, SharedAppMessages.DASHBOARD_DATA_CHANGED, null, cancellationToken);

// OR: Simplified with Publish extension method (internally uses SendAsync)
await appHubContext.Clients.Group("AuthenticatedClients").Publish(SharedAppMessages.DASHBOARD_DATA_CHANGED, null, cancellationToken);
```

**Note**: The `Publish` extension method uses `SendAsync` internally and has the same fire-and-forget behavior. Both `SendAsync` and `Publish` work **without requiring special registration** in `AppClientCoordinator.cs` - they automatically bridge to PubSubService through the `PUBLISH_MESSAGE` handler.

### SignalR Messaging Targets

The server can send messages to different targets:

1. **`Clients.All()`**: All SignalR connections (authenticated or not)
2. **`Clients.Group("AuthenticatedClients")`**: All authenticated users (all their devices)
3. **`Clients.User(userId)`**: All devices of a specific user (web, mobile, desktop)
4. **`Clients.Client(connectionId)`**: A specific connection (one browser tab or app)

---

## 4. AppClientCoordinator - Orchestrating Everything

The **AppClientCoordinator** is responsible for initializing and coordinating all messaging services when the application starts.

**Location**: [`src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs`](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs)

### Key Responsibilities

1. **Initialize SignalR Connection**
2. **Subscribe to SignalR Events** (via `SubscribeToSignalRSharedAppMessages` method)
3. **Manage Authentication State Propagation**
4. **Handle Push Notification Subscriptions**
5. **Coordinate Telemetry Services**

### SignalR Event Subscriptions

The `SubscribeToSignalRSharedAppMessages` method registers handlers for SignalR messages:

#### PUBLISH_MESSAGE Handler

Bridges SignalR messages to PubSubService:

```csharp
hubConnection.On(SharedAppMessages.PUBLISH_MESSAGE, async (string message, object? payload) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
    PubSubService.Publish(message, payload);
    return true;
});
```

This is the foundation that allows server-side code to publish messages that client-side components can subscribe to.

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
        
    // Update telemetry context
    TelemetryContext.UserId = userId;
    TelemetryContext.UserSessionId = isAuthenticated ? user.GetSessionId() : null;

    // Update App Insights
    if (isAuthenticated)
        _ = appInsights.SetAuthenticatedUserContext(user.GetUserId().ToString());
    else
        _ = appInsights.ClearAuthenticatedUserContext();

    // Start SignalR connection
    await EnsureSignalRStarted();

    // Subscribe to push notifications
    await pushNotificationService.Subscribe(CurrentCancellationToken);

    // Update user session info
    if (isAuthenticated)
        await UpdateUserSession();

    lastPropagatedUserId = userId;
}
```

This ensures all services are updated when authentication state changes.

---

## 5. Push Notifications

### Push Notification Architecture

Push notifications enable you to send messages to users **even when the app is not running**. This is crucial for engaging users and delivering timely information.

The Boilerplate project supports **push notifications** across all platforms:

- **Web (Browser)**: Web Push API with VAPID
- **Android**: Firebase Cloud Messaging (FCM)
- **iOS**: Apple Push Notification Service (APNS)
- **macOS**: Apple Push Notification Service (APNS)

### Key Feature: Deep Linking

One of the most powerful features of push notifications in this project is **deep linking**. When you send a push notification, you can specify what happens when a user clicks on it.

**Using the `pageUrl` parameter**, you can:
- Open a specific page in your app when the notification is clicked
- Navigate users to relevant content (e.g., a new product, a promotion, an announcement)
- Create targeted marketing campaigns
- Announce new features and guide users directly to them

**Example**:

```csharp
await pushNotificationService.RequestPush(
    title: "New Product Available!",
    message: "Check out our latest product",
    pageUrl: "/products/123",  // Opens product detail page
    cancellationToken: cancellationToken
);
```

When the user clicks this notification:
- **Web**: The app opens and navigates to `/products/123`
- **Mobile (Android/iOS)**: The native app opens and navigates to `/products/123`
- **Desktop (Windows/macOS)**: The browser opens and navigates to `/products/123`

This is **extremely useful for**:
- **Marketing campaigns**: "Flash sale on electronics - 50% off!" → Opens sale page
- **New features**: "Try our new AI assistant!" → Opens AI assistant page
- **User engagement**: "You have 3 unread messages" → Opens messages page
- **Reminders**: "Complete your profile to unlock features" → Opens profile page

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

---

## 6. Bit.Butil.Notification - Browser Notification API

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

## 7. Testing Push Notifications - Understanding the Four Scenarios

When testing push notifications, it's critical to understand that there are **four distinct scenarios** based on the app state when the notification is sent and when the user taps on it. The Boilerplate project handles all four scenarios across all platforms.

✅ **Scenario 1**: Close the app completely → Send push notification → Tap the notification → Verify the app opens to the correct page

✅ **Scenario 2**: Close the app → Send push notification → Open the app manually (without tapping notification) → Now tap the notification → Verify navigation works

✅ **Scenario 3**: Keep the app open → Send push notification → Close the app → Tap the notification → Verify the app opens to the correct page

✅ **Scenario 4**: Keep the app open → Send push notification → Tap the notification immediately → Verify navigation works without restarting the app

### Key Takeaways

- The codebase includes specialized handling for all four push notification scenarios
- Different entry points are used depending on the app state (e.g., `OnCreate` vs `OnNewIntent` on Android)
- Service workers on the web platform handle scenario detection automatically using `clients.matchAll()`

---