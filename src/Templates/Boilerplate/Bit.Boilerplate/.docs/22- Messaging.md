# Stage 22: Messaging

Welcome to Stage 22! In this stage, you will learn about the comprehensive **messaging and real-time communication system** built into the Boilerplate project. This system provides multiple communication channels for different scenarios, from in-app component communication to real-time server updates and push notifications.

---

## 📋 Topics Covered

1. **In-App Messaging with PubSubService**
   - Publish-Subscribe Pattern
   - Real-World Example: Profile Picture Updates
   - Cross-Component Communication

2. **AppJsBridge - JavaScript-to-C# Communication**
   - Bridging JavaScript and C# Code
   - Publishing Messages from JavaScript
   - Publishing Messages from Web Service Workers
   - window.postMessage Integration

3. **SignalR Real-Time Communication**
   - SignalR Hub Architecture
   - Server-to-Client Messaging
   - Targeting Specific Clients, Groups, or Users
   - Client-to-Server Invocations

4. **AppClientCoordinator - Orchestrating Everything**
   - SignalR Event Subscriptions
   - Authentication State Propagation
   - Coordinating All Messaging Services

5. **Push Notifications**
   - Platform-Specific Push Notification Services
   - Deep Linking - Opening Specific Pages
   - Web Push (VAPID)
   - Native Push (Android, iOS, Windows, macOS)
   - Subscription Management

6. **Browser Notification API**
   - Bit.Butil.Notification Integration
   - Permission Management
   - Local Notifications

7. **Practical Examples**
   - Session Revocation Example
   - Dashboard Data Changed Example

---

## 1. In-App Messaging with PubSubService

### What is PubSubService?

The **PubSubService** implements a publish/subscribe pattern that enables **decoupled communication** between different parts of the application. It allows components to communicate without having direct references to each other, maintaining **loose coupling** and improving maintainability.

**Location**: [`src/Client/Boilerplate.Client.Core/Services/PubSubService.cs`](/src/Client/Boilerplate.Client.Core/Services/PubSubService.cs)

### Real-World Example: Profile Picture Updates

One of the most common use cases demonstrates the power of PubSubService:

**Scenario**: When a user changes their profile picture in the Settings/Profile page, the profile picture in the Header is automatically updated without any direct coupling between these components.

**How it works**:

1. **Publishing the Update** (from `ProfileSection.razor.cs`):

```csharp
// After successfully updating the profile
PubSubService.Publish(ClientAppMessages.PROFILE_UPDATED, CurrentUser);
```

2. **Subscribing to Updates** (from `MainLayout.razor.cs`):

```csharp
unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.PROFILE_UPDATED, async payload =>
{
    currentUser = payload is JsonElement jsonDocument
                    ? jsonDocument.Deserialize(jsonSerializerOptions.GetTypeInfo<UserDto>())! // PROFILE_UPDATED can be invoked from server through SignalR
                    : (UserDto)payload;
    await InvokeAsync(StateHasChanged);
}));
```

This pattern ensures that:
- The Settings page doesn't need to know about the Header component
- The Header component doesn't need to pull for updates
- New components can easily subscribe to profile updates
- The code remains maintainable and testable

### Where PubSubService Can Be Used

PubSubService is versatile and can be used in multiple contexts:

- Within Blazor components and pages
- Outside Blazor components (e.g., MAUI XAML pages)
- From JavaScript code using `window.postMessage` (via AppJsBridge)
- From server-side code using SignalR

**Persistent Messages**: If `persistent = true`, the message is stored and delivered to handlers that subscribe **after** the message was published. This is useful for scenarios where a component needs data that was published before it was created.

### Message Types

#### ClientAppMessages

**Location**: [`src/Client/Boilerplate.Client.Core/Services/ClientAppMessages.cs`](/src/Client/Boilerplate.Client.Core/Services/ClientAppMessages.cs)

These messages are for **client-only** pub/sub communication:

```csharp
public partial class ClientAppMessages : SharedAppMessages
{
    public const string PROFILE_UPDATED = nameof(PROFILE_UPDATED);
    // ... and more
}
```

#### SharedAppMessages

**Location**: [`src/Shared/Services/SharedAppMessages.cs`](/src/Shared/Services/SharedAppMessages.cs)

These messages are used for **server-to-client communication** through SignalR:

```csharp
public partial class SharedAppMessages
{
    public const string DASHBOARD_DATA_CHANGED = nameof(DASHBOARD_DATA_CHANGED);
    // ... and more
}
```

**Note**: `ClientAppMessages` inherits from `SharedAppMessages`, so it includes both client-only and server-to-client messages.

---

## 2. AppJsBridge - JavaScript-to-C# Communication

### What is AppJsBridge?

**AppJsBridge** is a component that enables bidirectional communication between JavaScript/TypeScript code and C# .NET code. This is particularly useful for:

- Integrating third-party JavaScript libraries
- Handling browser events that need C# processing
- Enabling service workers to communicate with the Blazor app
- Publishing messages from JavaScript to the PubSubService

**Location**: [`src/Client/Boilerplate.Client.Core/Components/Layout/AppJsBridge.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/AppJsBridge.razor.cs)

#### AppJsBridge Usage Examples From JavaScript Code

```javascript
// Publish a message to PubSubService
App.publishMessage('CUSTOM_EVENT', { data: 'some data' });

// Show diagnostic modal by publishing `SHOW_DIAGNOSTIC_MODAL` message to C# PubSubService
App.showDiagnostic();
```

---

## 3. SignalR Real-Time Communication

### What is SignalR?

**SignalR** is ASP.NET Core's real-time communication library that enables bi-directional communication between server and clients. The Boilerplate project uses SignalR to:

- Send messages from server to specific clients, groups, or all clients
- Invoke client-side methods from the server

### AppHub - The SignalR Hub

The main SignalR hub is located at:

**Location**: [`src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs`](/src/Server/Boilerplate.Server.Api/SignalR/AppHub.cs)

### Hub Capabilities and Messaging Targets

The `AppHub` provides enhanced messaging capabilities beyond basic SignalR. The server can send messages to:

1. **`Clients.All()`**: Broadcasts to all SignalR connections (authenticated or not)
2. **`Clients.Group("AuthenticatedClients")`**: Sends messages to all signed-in browser tabs and apps
3. **`Clients.User(userId)`**: Sends messages to **all devices of a specific user** (a user might have multiple sessions - web app open twice, mobile app, desktop app, etc.)
4. **`Clients.Client(connectionId)`**: Targets a **specific connection** (e.g., a specific browser tab or app instance)

**Understanding Multi-Device Targeting**: When you use `Clients.User(userId)`, SignalR sends the message to ALL devices and sessions where that user is signed in.

### Message Types

**Key Message Types**:

1. **`PUBLISH_MESSAGE`**: Bridges SignalR and PubSubService
   - Server sends this event to publish a message on the client's PubSubService
   - Example: `SharedAppMessages.SESSION_REVOKED` - Redirects the device to the Sign In page when a session is revoked
   - Example: `SharedAppMessages.DASHBOARD_DATA_CHANGED` - Notifies clients to refresh dashboard data

2. **`SHOW_MESSAGE`**: Displays a text message to the user
   - Shows as a browser notification if available
   - Falls back to snackbar if notifications aren't available
   - Can include custom data for handling notification clicks

3. **`EXCEPTION_THROWN`**: Pushes exceptions to the client for display
   - Allows server to notify clients of hangfire background job errors
   - Uses the same exception handling UI as client-side errors


#### Authentication State Management

Each user session tracks its **SignalR connection ID** in the database. This enables powerful scenarios like:

**Example**: When an admin revokes a user session, the server can send a SignalR message directly to that specific browser tab or app:

```csharp
// From UserController.cs - RevokeSession method
_ = await appHubContext.Clients.Client(userSession.SignalRConnectionId)
    .InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, message, null, cancellationToken);
```

This ensures the corresponding browser tab or app immediately:
- Clears its access/refresh tokens from storage
- Navigates to the sign-in page automatically

### SendAsync (Publish) vs InvokeAsync

Understanding when to use `SendAsync` vs `InvokeAsync` is crucial for reliable server-to-client communication:

#### When to Use InvokeAsync

Use `InvokeAsync` when:
- The server is sending a message to a **specific SignalR connection ID**
- It's **important to know** if the message arrived on the client side or not
- You need to wait for a response from the client
- You need confirmation that the operation completed successfully

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

#### When to Use SendAsync (Publish)

Use `SendAsync` when:
- Broadcasting to multiple clients (e.g., `Clients.All()`, `Clients.Group()`)
- You don't need confirmation of delivery
- Fire-and-forget messaging is acceptable

**Example**:

```csharp
// Notify all authenticated clients - no need to wait for confirmation
await appHubContext.Clients.Group("AuthenticatedClients")
    .SendAsync(SharedAppMessages.PUBLISH_MESSAGE, 
               SharedAppMessages.DASHBOARD_DATA_CHANGED, 
               null, 
               cancellationToken);

// OR: Simplified with `Publish` extension method:               
await appHubContext.Clients.Group("AuthenticatedClients")
    .Publish(SharedAppMessages.DASHBOARD_DATA_CHANGED, 
               null, 
               cancellationToken);
```

#### Making InvokeAsync Work

For `InvokeAsync` to work properly, the client-side `hubConnection.On` listeners (located in `AppClientCoordinator`) must return a value (typically `true`) to acknowledge receipt:

```csharp
// Client-side in AppClientCoordinator.cs
hubConnection.On<string, Dictionary<string, string?>?, bool>(SharedAppMessages.SHOW_MESSAGE, async (message, data) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to show.", message);
    
    // ... show message logic ...
    
    return true; // This return value enables InvokeAsync on the server
});
```

### SignalR Hub Configuration

The SignalR hub is configured in [`src/Server/Boilerplate.Server.Api/Program.Middlewares.cs`](/src/Server/Boilerplate.Server.Api/Program.Middlewares.cs):

```csharp
app.MapHub<SignalR.AppHub>("/app-hub", options => options.AllowStatefulReconnects = true ...);
```

### Client-Side SignalR Connection

The SignalR connection is configured on the client side in:

**Location**: [`src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IClientCoreServiceCollectionExtensions.cs)

```csharp
var hubConnection = new HubConnectionBuilder()
    .WithStatefulReconnect()
    .WithAutomaticReconnect(...)
    .WithUrl(...)
    .Build();
```
---

## 4. AppClientCoordinator - Orchestrating Everything

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
hubConnection.On<string, Dictionary<string, string?>?, bool>(SharedAppMessages.SHOW_MESSAGE, async (message, data) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to show.", message);
    
    ...
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
    _ = await appHubContext.Clients.Client(userSession.SignalRConnectionId)
        .InvokeAsync<bool>(SharedAppMessages.SHOW_MESSAGE, 
                   (string)Localizer[nameof(AppStrings.TestNotificationMessage2)], 
                   null, 
                   cancellationToken);
}
```

#### PUBLISH_MESSAGE Event

```csharp
hubConnection.On(SharedAppMessages.PUBLISH_MESSAGE, async (string message, object? payload) =>
{
    logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
    PubSubService.Publish(message, payload);
    return true;
});
```

This bridges **SignalR** and **PubSubService**, allowing server-side code to publish messages that client-side components can subscribe to.

#### EXCEPTION_THROWN Event

```csharp
hubConnection.On(SharedAppMessages.EXCEPTION_THROWN, async (AppProblemDetails appProblemDetails) =>
{
    ExceptionHandler.Handle(appProblemDetails, displayKind: ExceptionDisplayKind.NonInterrupting);
    return true;
});
```

This allows the server to push exceptions to the client for display (e.g., showing an error that occurred in a hangfire background job).

#### UPLOAD_DIAGNOSTIC_LOGGER_STORE Method

```csharp
hubConnection.On(SharedAppMessages.UPLOAD_DIAGNOSTIC_LOGGER_STORE, async () =>
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

## 8. Dashboard Data Changed Example

Another common scenario is notifying all authenticated clients when data changes.

### Scenario: Product is Created/Updated/Deleted

**Server-Side** (from `ProductController.cs`):

```csharp
private async Task PublishDashboardDataChanged(CancellationToken cancellationToken)
{
    // Notify all authenticated clients
    await appHubContext.Clients.Group("AuthenticatedClients")
        .SendAsync(SharedAppMessages.PUBLISH_MESSAGE, 
                   SharedAppMessages.DASHBOARD_DATA_CHANGED, 
                   null, 
                   cancellationToken);
}
```

This is called after creating, updating, or deleting a product.

**Client-Side** (any component can subscribe):

```csharp
unsubscribe = PubSubService.Subscribe(SharedAppMessages.DASHBOARD_DATA_CHANGED, async (_) =>
{
    // Refresh dashboard data
    await LoadDashboardData();
    await InvokeAsync(StateHasChanged);
});
```

Components subscribe to `DASHBOARD_DATA_CHANGED` and automatically refresh when notified.

---

### AI Wiki: Answered Questions
* [Describe the workflow of bit Boilerplate's AI chat feature and provide a high-level overview.
](https://deepwiki.com/search/describe-the-workflow-of-bit-b_822b9510-8e1d-456f-99bf-fb1778374a9a)

Ask your own question [here](https://wiki.bitplatform.dev)

---