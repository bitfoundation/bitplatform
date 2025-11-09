# Stage 23: Diagnostic Modal

The **Diagnostic Modal** is an incredibly powerful built-in troubleshooting tool that provides developers and support teams with comprehensive runtime information about the application. This feature is available in **all environments** (Development, Staging, and Production) and on **all platforms** (Web, Android, iOS, Windows, macOS).

## What is the Diagnostic Modal?

Located at [`src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor), the Diagnostic Modal is a special UI component that displays detailed diagnostic information about the running application in real-time. It serves as an **in-app troubleshooting console** that works even on mobile devices where traditional developer tools are unavailable.

## 🎯 Hands-On Exploration (STRONGLY RECOMMENDED)

**Before diving into the documentation**, we **highly recommend** experiencing the Diagnostic Modal firsthand:

1. Visit **https://bitplatform.dev/demos**
2. Open any published demo app (Admin Panel, Sales Dashboard, etc.)
3. Click **7 times** on the header OR press **Ctrl+Shift+X**
4. Explore all features:
   - View telemetry context
   - Filter and search logs
   - Test diagnostic API
   - Try utility buttons
   - View detailed log information

This hands-on experience will give you a much better understanding of the tool's capabilities and value than reading documentation alone.

---

## How to Access the Diagnostic Modal

There are **three ways** to open the Diagnostic Modal:

### 1. Click 7 Times on the Header Spacer
The easiest method for end-users or support staff:
- Click **7 times** on the spacer area in the application header
- Implementation: [`DiagnosticSpacer.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/Header/DiagnosticSpacer.razor.cs)

### 2. Keyboard Shortcut: Ctrl+Shift+X
For developers who prefer keyboard shortcuts:
- Press **`Ctrl+Shift+X`** from anywhere in the application
- Implementation: Registered in [`MainLayout.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.cs) using Butil Keyboard API

### 3. JavaScript Console Command
For developers using browser dev tools:
```javascript
App.showDiagnostic()
```
- Type this command in the browser's JavaScript console
- Works even in production builds

---

## Key Features

The Diagnostic Modal provides three main areas of functionality:

### 1. **Telemetry Context Viewer**

Displays comprehensive application context information including:
- **User Information**: User ID, email, roles, session details
- **Device Information**: Platform, OS version, browser/app version
- **Application State**: Environment (Dev/Staging/Production), culture/locale settings
- **Network Information**: Server address, connection status, IP address
- **Session Details**: Authentication status, session ID, last activity

**Actions Available:**
- **Copy to Clipboard**: Copy all telemetry context as formatted text for easy sharing with support teams

---

### 2. **In-App Log Viewer**

A powerful log viewing and filtering system that displays logs captured by the `DiagnosticLogger`:

#### Filtering & Search Capabilities
- **Text Search**: Search logs by message content, category, or state values
- **Regular Expression Support**: Enable regex mode for advanced pattern matching
- **Filter by Log Level**: Filter by Trace, Debug, Information, Warning, Error, Critical
- **Filter by Category**: Multi-select dropdown showing all log categories (e.g., `Microsoft.EntityFrameworkCore`, `Boilerplate.Client.Core.Services`)
- **Sort Order**: Toggle between ascending/descending chronological order

#### Log Details View
- **Copy Individual Logs**: Copy any log entry to clipboard
- **Detailed View**: Click "Details" to see full log information:
  - Category
  - Message
  - Exception details (if any)
  - State key-value pairs
- **Navigate Between Logs**: Use Previous/Next buttons to browse through filtered logs
- **Timestamp Display**: Each log shows time in HH:mm:ss format
- **Color-Coded Severity**: Visual distinction between log levels using BitColor theming

---

### 3. **Utility Buttons & Actions**

The modal provides several powerful diagnostic and maintenance actions:

#### 🔄 **Reload Logs**
- Refreshes the log viewer with the latest in-memory logs
- Useful when logs are added while the modal is open

#### 🗑️ **Clear Logs**
- Clears all logs from in-memory storage
- Useful for starting fresh diagnostic sessions

#### ⚠️ **Throw Test Exception**
- Generates a test exception to verify error handling
- Alternates between `InvalidOperationException` (unknown exception) and `DomainLogicException` (known exception)
- Both include test data to demonstrate exception data capture
- **Use Case**: Testing error boundaries, exception handlers, and logging infrastructure

#### 🔬 **Call Diagnostics API**
- Sends a request to [`DiagnosticsController.PerformDiagnostics`](/src/Server/Boilerplate.Server.Api/Controllers/Diagnostics/DiagnosticsController.cs)
- Returns comprehensive server-side diagnostics including:
  - Client IP address
  - HTTP trace identifier
  - Authentication status
  - All HTTP request headers
  - Server environment name
  - Base URLs
  - Runtime information (AOT detection, GC configuration, etc.)
- **Also Tests**:
  - Push notification functionality (if subscribed)
  - SignalR connection (if connected)
- Displays results in a BitMessageBox

#### 🛠️ **Open Dev Tools** (Mobile & Desktop Apps)
- Opens an **in-app browser DevTools** interface
- **Critical Feature**: Works on **mobile devices** (Android/iOS) where traditional DevTools are unavailable
- Provides console, network inspector, and other debugging tools
- Implementation: Uses `App.openDevTools()` JavaScript interop

#### ♻️ **Call GC** (Non-Browser Platforms Only)
- Forces garbage collection on .NET MAUI and Windows platforms
- Shows memory usage **before** and **after** GC using SnackBar notifications
- Performs aggressive GC with compaction:
  ```csharp
  GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
  GC.WaitForPendingFinalizers();
  ```
- **Use Case**: Testing memory management and investigating memory leaks

#### 🧹 **Clear Cache**
- Comprehensive cache clearing operation:
  - Deletes all WebAuthn credentials
  - Signs out the current user
  - Clears all local/session storage
  - Removes all cookies
  - **Web**: Uninstalls service worker and clears cache storages using `BitBswup.forceRefresh()`
  - **Hybrid Apps**: Forces a full application reload
- **Use Case**: Reset app state for troubleshooting or testing fresh installations

#### 🔄 **Update App**
- Forces immediate app update check and installation
- Uses `IAppUpdateService.ForceUpdate()`
- Bypasses normal update schedules
- **Use Case**: Testing force update system or applying urgent fixes

---

## Real-World Use Cases

### 1. **Remote Support Scenarios**
When a user reports an issue:
1. Ask them to click 7 times on the header
2. Have them copy the **Telemetry Context** and send it to you
3. Review their logs to see what errors occurred
4. Test their push notifications and SignalR connectivity via **Call Diagnostics API**

### 2. **Mobile App Debugging**
On iOS or Android where DevTools aren't available:
1. Open Diagnostic Modal (7 clicks)
2. Click **Open Dev Tools** to access in-app browser DevTools
3. View console logs, network requests, and inspect DOM elements
4. Test exception handling with **Throw Test Exception**

### 3. **Performance Investigation**
To investigate memory issues:
1. Open Diagnostic Modal
2. Note current memory usage from Telemetry Context
3. Use **Call GC** button to force garbage collection
4. Compare memory before/after to identify potential leaks

### 4. **Authentication Troubleshooting**
When users report login issues:
1. View authentication status in Telemetry Context
2. Check session information
3. Review authentication-related logs
4. Use **Call Diagnostics API** to verify server-side authentication state

### 5. **Testing in Production**
Even in production environments:
- Support staff can access diagnostic information without special builds
- No need to deploy debug versions
- Users can provide detailed diagnostic info without technical knowledge

---

## Technical Implementation Details

### Architecture

The Diagnostic Modal uses a **Pub/Sub pattern** for activation:
- **Message**: `ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL`
- **Publisher**: `DiagnosticSpacer`, `MainLayout`, or JavaScript interop
- **Subscriber**: `AppDiagnosticModal` component

### Dependency Injection

The modal uses `[AutoInject]` to inject required services:
```csharp
[AutoInject] private Clipboard clipboard = default!;
[AutoInject] private HubConnection hubConnection = default!;
[AutoInject] private ITelemetryContext telemetryContext = default!;
[AutoInject] private BitMessageBoxService messageBoxService = default!;
[AutoInject] private IDiagnosticsController diagnosticsController = default!;
[AutoInject] private IPushNotificationService pushNotificationService = default!;
```

### Related Files

| File | Purpose |
|------|---------|
| [`AppDiagnosticModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor) | Main UI component |
| [`AppDiagnosticModal.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor.cs) | Component logic, log filtering |
| [`AppDiagnosticModal.razor.Utils.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor.Utils.cs) | Utility methods (GC, cache clear, etc.) |
| [`DiagnosticLogModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/DiagnosticLogModal.razor) | Detailed log view modal |
| [`DiagnosticSpacer.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/Header/DiagnosticSpacer.razor.cs) | 7-click activation handler |
| [`DiagnosticsController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Diagnostics/DiagnosticsController.cs) | Server-side diagnostics API |
| [`DiagnosticLogger.cs`](/src/Client/Boilerplate.Client.Core/Services/DiagnosticLog/DiagnosticLogger.cs) | In-memory log storage |

---

## Best Practices

### For Developers

1. **Use in Development**: Keep the modal open while developing to monitor logs in real-time
2. **Test Exception Handling**: Use "Throw Test Exception" to verify your error boundaries work correctly
3. **Monitor Memory**: Use "Call GC" and telemetry context to track memory usage during testing
4. **Verify Updates**: Use "Update App" to test your force update system

### For Support Teams

1. **First Step in Troubleshooting**: Always ask users to open the Diagnostic Modal (7 clicks)
2. **Gather Context Early**: Request telemetry context copy before attempting fixes
3. **Check Logs**: Look for error-level logs to identify the root cause
4. **Test Connectivity**: Use "Call Diagnostics API" to verify push notifications and SignalR

### For End Users

1. **Easy Access**: Just click 7 times on the header - no technical knowledge required
2. **Copy and Share**: Use the copy buttons to send information to support
3. **Safe to Use**: All actions are safe and won't harm the application
4. **Production Ready**: Available in production without special setup

---

## Key Takeaways

✅ **Available Everywhere**: Works in all environments and on all platforms (including mobile)  
✅ **No Special Builds Needed**: Available in production without debug builds  
✅ **User-Friendly Access**: 7 clicks makes it accessible to non-technical users  
✅ **Comprehensive Information**: Telemetry, logs, and runtime diagnostics in one place  
✅ **Powerful Utilities**: Test exceptions, clear cache, force GC, update app, and more  
✅ **Mobile DevTools**: In-app browser DevTools work even on iOS/Android  
✅ **Remote Support**: Users can easily share diagnostic information with support teams  
✅ **Real-Time Monitoring**: View logs and application state as they happen  

The Diagnostic Modal is one of the most valuable troubleshooting tools in the boilerplate, significantly reducing the time needed to diagnose and resolve issues in both development and production environments.

---