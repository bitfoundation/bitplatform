# Stage 23: Diagnostic Modal

The **Diagnostic Modal** is an incredibly powerful built-in troubleshooting tool that provides developers and support teams with comprehensive runtime information about the application. The modal itself is available in **all environments** (Development, Staging, and Production) and on **all platforms** (Web, Android, iOS, Windows, macOS). Its **log list** additionally needs the `DiagnosticLogger` provider, which is registered on the client runtimes (Blazor WebAssembly, MAUI, Windows) in every environment, and on a Blazor Server host only in Development.

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
- **Filter by Category**: Multi-select dropdown showing all log categories (e.g., `Microsoft.EntityFrameworkCore`, `Boilerplate.Client.Core.Infrastructure.Services`)
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
- Sends a request to [`DiagnosticController.PerformDiagnostic`](/src/Server/Boilerplate.Server.Api/Features/Diagnostic/DiagnosticController.cs)
- Returns comprehensive server-side diagnostics including:
  - Client IP address
  - HTTP trace identifier
  - Authentication status
  - Current culture and UI culture
  - All HTTP request headers
  - Server environment name, and (when `multitenant` is on) the current `TenantId`
  - Base URLs
- The modal then appends runtime information of its own (AOT detection, GC configuration, etc.) - that part is produced by the component, not by the API
- **Also Tests**:
  - Push notification functionality (if subscribed)
  - SignalR connection (if connected)
- Displays results in a BitMessageBox

#### 🛠️ **Open Dev Tools**
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

#### 🧹 **Clear App Files**
- Comprehensive app storage clearing operation:
  - Deletes all WebAuthn credentials
  - Signs out the current user
  - Clears all local/session storage
  - **Web**: Uninstalls service worker and clears cache storages using `BitBswup.forceRefresh()`
  - **Hybrid Apps**: Forces a full application reload
- **Use Case**: Reset app state for troubleshooting or testing fresh installations

#### 🔄 **Update App**
- Forces immediate app update check and installation
- Uses `IAppUpdateService.ForceUpdate()`
- Bypasses normal update schedules
- **Use Case**: Testing force update system or applying urgent fixes

---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
