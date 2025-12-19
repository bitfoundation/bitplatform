# Stage 24: WebAuthn and Passwordless Authentication (Advanced)

Welcome to Stage 24! In this stage, you'll learn about the advanced WebAuthn implementation in the Boilerplate project, which enables secure passwordless authentication using biometric methods like fingerprint, Face ID, and PIN across all platforms.

---

## 🔐 What is WebAuthn?

**WebAuthn** (Web Authentication) is a modern web standard that enables passwordless authentication using public-key cryptography. The Boilerplate project implements a comprehensive WebAuthn solution that works across:

- **Web browsers** (Chrome, Edge, Firefox, Safari)
- **Android devices** (fingerprint, face unlock, PIN)
- **iOS devices** (Touch ID, Face ID, passcode)
- **Windows** (Windows Hello - fingerprint, face recognition, PIN)
- **macOS** (Touch ID, password)

> **Note**: Face ID is currently not supported on Android devices due to platform limitations.

---

## 🏗️ Architecture Overview

The WebAuthn implementation in this project consists of several key components working together:

### High-Level Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Components                         │
│  (SignInPanel, PasswordlessTab - User Interface)            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              WebAuthnServiceBase (Abstract)                  │
│  - Manages user IDs in local storage                        │
│  - Defines interface for platform implementations           │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────┼────────────┬
        ▼            ▼            ▼
┌──────────┐  ┌──────────┐  ┌──────────┐
│   Web    │  │   MAUI   │  │ Windows  │
│ Service  │  │ Service  │  │ Service  │
└────┬─────┘  └────┬─────┘  └────┬─────┘
     │             │              │
     │             │              │  (Blazor Hybrid needs special handling)
     │             │              │
     │             ▼              ▼
     │    ┌─────────────────────────────────┐
     │    │   ILocalHttpServer               │
     │    │   - Starts local HTTP server     │
     │    │   - Serves web-interop-app.html  │
     │    └──────────┬──────────────────────┘
     │               │
     │               ▼
     │    ┌─────────────────────────────────┐
     │    │ IExternalNavigationService       │
     │    │ - Opens in-app browser           │
     │    └──────────┬──────────────────────┘
     │               │
     │               ▼
     │    ┌─────────────────────────────────┐
     │    │  web-interop-app.html            │
     │    │  (Lightweight page with app.js)  │
     │    │  - Runs on localhost origin      │
     │    │  - Calls WebAuthn APIs           │
     │    │  - Returns results via HTTP      │
     │    └──────────┬──────────────────────┘
     │               │
     └───────────────┴──────────────────────┐
                                             │
                                             ▼
                              ┌─────────────────────────────┐
                              │   Server API Controllers     │
                              │  - UserController.WebAuthn   │
                              │  - IdentityController.WebAuthn│
                              └──────────┬──────────────────┘
                                         │
                                         ▼
                              ┌─────────────────────────────┐
                              │   Fido2NetLib (Server)      │
                              │  - Validates challenges      │
                              │  - Verifies attestations     │
                              └──────────┬──────────────────┘
                                         │
                                         ▼
                              ┌─────────────────────────────┐
                              │   Database                  │
                              │  - WebAuthnCredential table │
                              │  - Stores public keys       │
                              └─────────────────────────────┘
```

---

## 🔄 WebAuthn Flow: Step-by-Step

### Registration Flow (Creating a Credential)

When a user wants to **enable passwordless authentication**, here's what happens:

**1. User Initiates Registration**
   - User navigates to Settings → Account → Passwordless tab
   - Clicks "Enable Passwordless Sign-In"

**2. Client Requests Options**
   ```
   Client → Server: GET /api/User/GetWebAuthnCredentialOptions
   ```

**3. Server Generates Challenge** (`UserController.WebAuthn.cs`)
   - Creates a cryptographic challenge (random bytes)
   - Retrieves user information (ID, display name)
   - Generates credential creation options using Fido2NetLib
   - Stores options in distributed cache (expires in 3 minutes)
   - Returns options to client

**4. Platform-Specific Credential Creation**

   **Web Platform** (`WebAuthnService.cs`):
   - Directly calls browser's WebAuthn API via Bit.Butil
   - Browser shows native biometric prompt
   
   **Blazor Hybrid** (`MauiWebAuthnService.cs`, `WindowsWebAuthnService.cs`):
   - **Challenge**: WebView has IP-based origin (`http://0.0.0.1`) - WebAuthn requires **valid origin**
   - **Solution**: Use Local HTTP Server + In-App Browser
     1. Start local HTTP server on available port on user's device (e.g., `http://localhost:54321`)
     2. Navigate **in-app browser** (Not the webview) to `http://localhost:54321/web-interop-app.html?actionName=CreateWebAuthnCredential`
     3. `web-interop-app.html` runs on proper `localhost` origin
     4. Fetches options from local server: `GET /api/GetCreateWebAuthnCredentialOptions`
     5. Calls WebAuthn API: `Bit.Butil.webAuthn.createCredential(options)`
     6. Device shows native biometric prompt
     7. Posts result back: `POST /api/CreateWebAuthnCredential`
     8. Local server completes TaskCompletionSource
     9. In-app browser closes automatically

**5. Client Sends Attestation** to Server
   ```
   Client → Server: PUT /api/User/CreateWebAuthnCredential
   ```

**6. Server Validates and Stores** (`UserController.WebAuthn.cs`)
   - Retrieves cached options using challenge
   - Validates attestation response using Fido2NetLib
   - Verifies credential is unique
   - Creates `WebAuthnCredential` entity with:
     - User ID
     - Credential ID
     - Public key
     - Transport methods (USB, NFC, BLE, Internal)
     - Backup eligibility flags
   - Saves to database
   - Removes options from cache

**7. Client Updates Local Storage**
   - Stores user ID in local storage under `bit-webauthn` key
   - Used later to show "Sign in with Passwordless" option in sign-in page

---

### Authentication Flow (Using a Credential)

When a user signs in with passwordless authentication:

**1. User Initiates Sign-In**
- Client checks if WebAuthn is available and configured (See previous steps)
- Shows "Sign in with Fingerprint/Face ID" button

**2. Client Requests Assertion Options**
   ```
   Client → Server: POST /api/Identity/GetWebAuthnAssertionOptions
   Body: { "UserIds": [<guid>, ...] }
   ```

**3. Server Generates Assertion Options** (`IdentityController.WebAuthn.cs`)
   - Queries database for credentials belonging to user IDs
   - Creates list of allowed credentials (credential IDs + transports)
   - Generates assertion options with challenge
   - Stores options in distributed cache (expires in 3 minutes)
   - Returns options to client

**4. Platform-Specific Credential Retrieval**

   **Web Platform**:
   - Calls browser's `navigator.credentials.get()`
   - Browser shows credential picker + biometric prompt
   
   **Blazor Hybrid**:
   - Same Local HTTP Server + In-App Browser pattern
   - Navigates to `web-interop-app.html?actionName=GetWebAuthnCredential`
   - Fetches options and calls WebAuthn API
   - Returns assertion via local HTTP endpoint

**5. Client Chooses Sign-In Method**

   The user can complete authentication in two ways:

   **Option A: Direct Sign-In** (2FA disabled)
   ```
   Client → Server: POST /api/Identity/VerifyWebAuthAndSignIn
   Body: { 
     "ClientResponse": <assertion>,
     "TfaCode": "123456" (optional)
   }
   ```

   **Option B: Request 2FA Code** (2FA enabled, need code)
   ```
   Client → Server: POST /api/Identity/VerifyWebAuthAndSendTwoFactorToken
   Body: { "ClientResponse": <assertion> }
   ```
   - Server verifies assertion
   - Sends 2FA code via configured method
   - User then signs in with the code

**6. Server Verifies Assertion** (`IdentityController.WebAuthn.cs`)
   - Retrieves cached options using challenge
   - Finds credential in database by credential ID
   - Validates assertion using Fido2NetLib:
     - Verifies signature with stored public key
     - Checks signature counter (prevents replay attacks)
     - Validates user handle matches credential owner
   - Updates signature counter in database
   - Generates automatic sign-in OTP (6-digit code)
   - Completes sign-in or sends 2FA token

**7. User is Authenticated**
   - JWT tokens issued
   - Session created in database
   - User redirected to application

---

## 🧩 Key Components Explained

### 1. WebAuthn Data Model

**`WebAuthnCredential` Entity** (`Boilerplate.Server.Api/Models/Identity/WebAuthnCredential.cs`):

Stores credential information in the database:

```csharp
public class WebAuthnCredential
{
    public byte[] Id { get; set; }              // Credential ID (unique identifier)
    public Guid UserId { get; set; }            // Owner of this credential
    public byte[]? PublicKey { get; set; }      // Public key for signature verification
    public uint SignCount { get; set; }         // Counter to prevent replay attacks
    public AuthenticatorTransport[]? Transports { get; set; }  // How credential can be accessed
    public bool IsBackupEligible { get; set; }  // Can be backed up to cloud
    public bool IsBackedUp { get; set; }        // Currently backed up
    public DateTimeOffset RegDate { get; set; } // When credential was created
    // ... additional metadata
}
```

### 2. Client-Side Service Hierarchy

**`WebAuthnServiceBase`** (`Boilerplate.Client.Core/Services/WebAuthnServiceBase.cs`):

Abstract base class managing user IDs in local storage:

```csharp
public abstract class WebAuthnServiceBase : IWebAuthnService
{
    // Platform-specific implementations must provide:
    public abstract ValueTask<bool> IsWebAuthnAvailable();
    public abstract ValueTask<JsonElement> CreateWebAuthnCredential(JsonElement options);
    public abstract ValueTask<JsonElement> GetWebAuthnCredential(JsonElement options);

    // Shared logic for all platforms:
    public async ValueTask<Guid[]> GetWebAuthnConfiguredUserIds() { /* ... */ }
    public async ValueTask<bool> IsWebAuthnConfigured(Guid? userId) { /* ... */ }
    public async ValueTask SetWebAuthnConfiguredUserId(Guid userId) { /* ... */ }
    public async ValueTask RemoveWebAuthnConfiguredUserId(Guid? userId) { /* ... */ }
}
```

**Platform Implementations**:

- **`WebAuthnService`** (Web): Directly uses Bit.Butil WebAuthn wrapper
- **`MauiWebAuthnService`** (MAUI): Uses Local HTTP Server pattern
- **`WindowsWebAuthnService`** (Windows): Uses Local HTTP Server pattern

### 3. The Local HTTP Server Pattern (Blazor Hybrid Only)

**Why is it needed?**

WebView in Blazor Hybrid uses an IP-based origin like `http://0.0.0.1`. WebAuthn requires a proper origin (e.g., `https://example.com` or `http://localhost`). Without a proper origin, WebAuthn APIs fail.

**How it works:**

**`ILocalHttpServer`** Interface (`Boilerplate.Client.Core/Services/Contracts/ILocalHttpServer.cs`):

```csharp
public interface ILocalHttpServer : IAsyncDisposable
{
    int EnsureStarted();  // Starts server and returns port
    int Port { get; }     // Port server is listening on
    string? Origin { get; } // Origin string (e.g., "http://localhost:54321")
}
```

**`MauiLocalHttpServer`** Implementation:

1. **Finds Available Port**: Uses TCP listener to find free port
2. **Starts EmbedIO Server**: Lightweight HTTP server embedded in the app
3. **Registers Endpoints**:
   - `GET /api/GetWebAuthnCredentialOptions` - Returns assertion options (for sign-in)
   - `POST /api/WebAuthnCredential` - Receives assertion result (for sign-in)
   - `GET /api/GetCreateWebAuthnCredentialOptions` - Returns credential creation options (for registration)
   - `POST /api/CreateWebAuthnCredential` - Receives attestation result (for registration)
   - `POST /api/LogError` - Receives JavaScript errors
   - `GET /*` - Serves static files (web-interop-app.html, app.js, etc.)
4. **Communicates Results**: Uses `TaskCompletionSource` to pass results back to calling code

**`IExternalNavigationService`** Interface:

```csharp
public interface IExternalNavigationService
{
    Task NavigateToAsync(string url);  // Opens URL in in-app browser
}
```

Platform implementations:
- **MAUI**: Uses `SFSafariViewController` (iOS) and `CustomTabs` (Android)
- **Windows**: Uses `WebView2` in a separate window

### 4. web-interop-app.html

**Location**: `Boilerplate.Client.Web/wwwroot/web-interop-app.html`

A lightweight HTML page that:

1. **Runs on proper origin** (`localhost` in Blazor Hybrid)
2. **Loads minimal JavaScript** (only `app.js`, not full Blazor)
3. **Handles WebAuthn operations**:
   - Fetches options from local server
   - Calls WebAuthn APIs
   - Posts results back to local server
   - Closes automatically

**Key Script** (`WebInteropApp.ts`):

```typescript
export class WebInteropApp {
    public static async run() {
        const action = urlParams.get('actionName');
        
        switch (action) {
            case 'GetWebAuthnCredential':
                await WebInteropApp.getWebAuthnCredential();
                break;
            case 'CreateWebAuthnCredential':
                await WebInteropApp.createWebAuthnCredential();
                break;
        }
        
        window.close(); // Auto-close after completion
    }
    
    private static async getWebAuthnCredential() {
        // Fetch assertion options from local server (for sign-in)
        const options = await fetch('api/GetWebAuthnCredentialOptions').json();
        
        // Call WebAuthn API (this is why we need proper origin!)
        const credential = await Bit.Butil.webAuthn.getCredential(options);
        
        // Post assertion result back
        await fetch('api/WebAuthnCredential', {
            method: 'POST',
            body: JSON.stringify(credential)
        });
    }
    
    // Similar for createWebAuthnCredential...
}
```

### 5. Server-Side Controllers

**`UserController.WebAuthn.cs`** - Credential Management:

- `GetWebAuthnCredentialOptions()` - Generate credential creation options
- `CreateWebAuthnCredential()` - Validate and store new credential
- `DeleteWebAuthnCredential()` - Remove a specific credential
- `DeleteAllWebAuthnCredentials()` - Remove all user's credentials

**`IdentityController.WebAuthn.cs`** - Authentication:

- `GetWebAuthnAssertionOptions()` - Generate assertion options for sign-in
- `VerifyWebAuthAndSignIn()` - Verify assertion and complete sign-in
- `VerifyWebAuthAndSendTwoFactorToken()` - Verify assertion and send 2FA code

### 6. Fido2NetLib Integration

The project uses the **Fido2NetLib** library for server-side WebAuthn operations:

**Configuration** (`Program.Services.cs`):

```csharp
services.AddFido2(options =>
{
    options.ServerName = "Boilerplate WebAuthn";
    options.ServerDomain = new Uri(serverAddress).Host;
    options.Origins = [serverAddress];
    options.TimestampDriftTolerance = serverSettings.Identity.BearerTokenExpiration;
});
```

**Key Operations**:

- `RequestNewCredential()` - Generates credential creation options
- `MakeNewCredentialAsync()` - Validates attestation response
- `GetAssertionOptions()` - Generates assertion options
- `MakeAssertionAsync()` - Validates assertion response

---