# Stage 23: Diagnostic Modal

## Overview

The **Diagnostic Modal** is a powerful built-in debugging and troubleshooting tool that provides developers and support staff with comprehensive diagnostic capabilities directly within the application. This modal is accessible across all platforms (Web, Android, iOS, Windows, macOS) and works in all environments (Development, Staging, Production).

**Location:** [`/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/Diagnostic/AppDiagnosticModal.razor)

## How to Access

The Diagnostic Modal can be opened in three ways:

1. **Click 7 times** on the spacer in the header (the empty space between the logo and menu items)
2. **Keyboard shortcut**: `Ctrl + Shift + X`
3. **JavaScript console**: Call `App.showDiagnostic()` in the browser's developer tools

## Key Features & Capabilities

### 1. Telemetry Context Viewer

**Purpose:** Displays comprehensive device and application information in a collapsible section.

**What it shows:**
- Device information (OS, browser, platform)
- Application version and environment
- User information (if authenticated)
- Session details
- Culture and language settings
- And much more contextual data

**Actions:**
- **Copy button**: Copies all telemetry data to clipboard for easy sharing with support teams

### 2. Real-Time Log Viewer

**Purpose:** Displays all application logs in real-time, both from the client-side and (in Development environment) server-side.

**Features:**
- **Live log streaming**: Shows logs as they occur
- **Color-coded log levels**: Visual distinction between Information (blue), Warning (yellow), Error/Critical (red)
- **Timestamps**: Each log entry shows the exact time it was created
- **Category display**: Shows which component or service generated the log
- **Virtualization**: Efficiently handles thousands of log entries without performance degradation

**Environment Behavior:**
- **Development**: Shows both client AND server logs for complete visibility
- **Production/Staging**: Shows only client-side logs for security reasons (prevents exposing sensitive server internals)

### 3. Advanced Log Filtering & Search

**Powerful filtering capabilities to find specific logs quickly:**

#### Search Features:
- **Text search**: Search through log messages, categories, and state data
- **RegEx support**: Enable Regular Expression mode for advanced pattern matching
- **Immediate/debounced**: Real-time search with 500ms debounce for performance

#### Filtering Options:
- **Log Level filter**: Multi-select dropdown to show/hide Trace, Debug, Information, Warning, Error, Critical
  - Default in **Development**: Shows Information, Warning, Error, Critical
  - Default in **Production/Staging**: Shows only Warning, Error, Critical (reduces noise)
- **Category filter**: Multi-select dropdown with search to filter by component/service name
  - Automatically populated from all unique categories in current logs
- **Sort**: Toggle between ascending/descending chronological order

### 4. Detailed Log Inspection

**Purpose:** View complete details of any log entry.

**How to use:** Click the "Details" button (📊 icon) on any log entry

**What it shows:**
- Full log message
- Category/source
- Complete exception details with stack traces (if applicable)
- State data (key-value pairs with contextual information)
- Timestamp

**Actions:**
- **Copy button**: Copy the full log details to clipboard
- **Navigation arrows**: Navigate to previous/next log entry without closing the modal

### 5. Throw Test Exception

**Purpose:** Test the application's exception handling system.

**How it works:**
- Click the **Error icon** button
- Alternates between throwing:
  - **Known Exception** (`DomainLogicException`): User-friendly error that's shown to users
  - **Unknown Exception** (`InvalidOperationException`): System error that shows different messages in Dev vs Production

**Use case:** Verify that exception handling, logging, and error display UI work correctly

### 6. Call Diagnostics API

**Purpose:** Performs comprehensive server-side diagnostics and tests real-time communication channels.

**What it does:**
- Sends diagnostic request to server with current connection details
- Tests SignalR connection (if enabled)
- Tests Push Notification subscription (if enabled)
- Retrieves server environment information
- Shows client IP address and request headers
- Displays culture and timezone information

**Use case:** Verify that client-server communication works correctly and check server environment configuration

### 7. Open In-App Dev Tools

**Purpose:** Opens browser-like developer tools **inside the application**.

**Key Feature:** Works even on **mobile devices** (Android/iOS) where traditional browser dev tools are not available!

**How to use:** Click the **Developer Tools icon** button

**What you can do:**
- Inspect HTML/CSS elements
- View console logs
- Debug JavaScript
- Monitor network requests
- All the capabilities of browser DevTools, but embedded in the app

**Use case:** Debug UI issues on mobile devices or in production builds where external dev tools aren't accessible

### 8. Clear Cache

**Purpose:** Completely reset the application state for troubleshooting.

**What it clears:**
- All stored data (localStorage, IndexedDB, etc.)
- Authentication tokens and session data
- WebAuthn credentials
- All cookies
- Service Worker cache (on Web)
- Forces a complete app refresh

**Use case:** Fix issues caused by corrupted cache or stale data. Essential when testing authentication flows or after significant app updates.

### 9. Call Garbage Collector (Non-Browser Only)

**Purpose:** Manually trigger .NET garbage collection and view memory usage.

**Platforms:** Available only on **Blazor Hybrid** platforms (Windows, MAUI - Android/iOS/macOS)

**What it does:**
- Shows current memory usage before GC
- Forces full garbage collection (Generation 2)
- Waits for pending finalizers
- Shows memory usage after GC
- Displays the amount of memory freed

**Use case:** Diagnose memory leaks or verify that objects are being properly disposed

### 10. Update App

**Purpose:** Manually trigger the force update mechanism.

**What it does:**
- **Web/Windows**: Immediately updates the app to the latest version and reloads
- **Android/iOS/macOS**: Opens the app store page for the user to update

**Use case:** Test the force update system or manually update to the latest version without waiting for automatic checks

### 11. Reload Logs

**Purpose:** Refresh the log list from the in-memory store.

**Use case:** If logs appear to be missing or out-of-date (though logs update automatically in most cases)

### 12. Clear Logs

**Purpose:** Remove all logs from the in-memory diagnostic log store.

**Use case:** Start with a clean slate when testing specific scenarios or reduce memory usage

### 13. Go to Top

**Purpose:** Quickly scroll back to the top of the log list.

**Position:** Floating button in the bottom-right corner of the modal

**Use case:** When viewing thousands of logs and need to return to the beginning

## Technical Architecture

### Client-Side Components

**Main Files:**
- `AppDiagnosticModal.razor`: The UI markup
- `AppDiagnosticModal.razor.cs`: Core diagnostic functionality and log viewing
- `AppDiagnosticModal.razor.Utils.cs`: Utility functions (cache clearing, GC, dev tools, etc.)

**Key Services Used:**
- `DiagnosticLogger`: In-memory log storage
- `ITelemetryContext`: Device and app context information
- `IDiagnosticsController`: HTTP client proxy for server diagnostics API
- `IPushNotificationService`: Push notification subscription details
- `HubConnection`: SignalR connection details
- `Clipboard`: For copying diagnostic data
- `IStorageService`: For cache clearing

### Server-Side Components

**File:** [`/src/Server/Boilerplate.Server.Api/Controllers/Diagnostics/DiagnosticsController.cs`](/src/Server/Boilerplate.Server.Api/Controllers/Diagnostics/DiagnosticsController.cs)

**Endpoint:** `GET /api/Diagnostics/PerformDiagnostics`

**What it provides:**
- Client IP address
- Request trace identifier
- Authentication status
- User session information
- Push notification subscription status
- SignalR connection verification
- All HTTP request headers
- Server environment name
- Base URLs

**Security:** Uses `[AllowAnonymous]` so it works even for unauthenticated users (important for troubleshooting sign-in issues)

## Real-World Use Cases

### For Developers

1. **Debugging in Development**:
   - View real-time logs from both client and server
   - Test exception handling
   - Verify server configuration
   - Monitor memory usage

2. **Testing in Production-like Environments**:
   - Use in-app dev tools on mobile devices
   - Verify push notifications and SignalR work correctly
   - Check that logs are properly filtered (no sensitive server logs)

3. **Performance Troubleshooting**:
   - Monitor memory consumption
   - Force garbage collection to identify leaks
   - View log categories to identify bottlenecks

### For Support Teams

1. **Remote Troubleshooting**:
   - Ask users to open the diagnostic modal (7 clicks on header)
   - Have them copy and send telemetry context
   - View logs to identify issues without needing server access

2. **Real-Time Support** (with SignalR enabled):
   - Support staff can automatically receive diagnostic logs via SignalR
   - Call `UPLOAD_DIAGNOSTIC_LOGGER_STORE` method to stream logs to support dashboard
   - No need to ask users to manually copy/paste logs

3. **Quick Fixes**:
   - Clear cache to resolve common issues
   - Force app update to ensure user is on latest version
   - Test push notifications to verify subscription

## Important Security Notes

⚠️ **Production Environment Considerations:**

1. **Server Logs Not Exposed**: In Production/Staging, only client-side logs are shown in the diagnostic modal. Server logs are never exposed to prevent leaking sensitive information.

2. **Known vs Unknown Exceptions**: 
   - **Known Exceptions**: Display user-friendly messages in all environments
   - **Unknown Exceptions**: Show detailed error messages only in Development; show generic "Unknown error" in Production

3. **Diagnostic API**: While marked `[AllowAnonymous]`, the diagnostic endpoint only returns non-sensitive information (headers, IP, environment name). Sensitive data remains protected.

4. **Support Access**: Support staff with remote access to user devices can view diagnostic information, but this requires appropriate access controls at the organizational level.

## Hands-On Recommendation

🎯 **Highly Recommended:** Visit https://bitplatform.dev/demos, open any published app, and test the Diagnostic Modal yourself:

1. Click 7 times on the header spacer (or press `Ctrl + Shift + X`)
2. Explore the telemetry context
3. View and filter the logs
4. Click the "Details" button on various log entries
5. Try the "Throw test error" button
6. Call the Diagnostics API
7. If on a mobile device, try opening the in-app dev tools
8. Test clearing the cache
9. Experiment with all the filtering options

**This hands-on experience will give you a much better understanding of the diagnostic capabilities than just reading the documentation!**

## Summary

The Diagnostic Modal is an **essential troubleshooting tool** that provides:

- ✅ Real-time log viewing with powerful filtering
- ✅ Comprehensive device and app telemetry
- ✅ Server diagnostics and connection testing
- ✅ In-app dev tools that work on mobile devices
- ✅ Memory monitoring and garbage collection
- ✅ Cache clearing and app update capabilities
- ✅ Support for remote troubleshooting scenarios
- ✅ Secure by design (no sensitive data exposure in production)

This tool significantly reduces the time needed to diagnose issues, especially in production environments where traditional debugging tools are not available.

---
