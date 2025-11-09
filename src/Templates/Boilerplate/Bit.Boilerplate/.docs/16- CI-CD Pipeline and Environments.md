# Stage 16: CI/CD Pipeline and Environments

Welcome to Stage 16! In this stage, you'll learn about the comprehensive CI/CD pipeline setup and environment configuration system that powers this project. This system provides environment-aware builds and deployments across all platforms.

---

## 📋 Environment Configuration System

### Understanding AppEnvironment

The project uses a unified environment configuration system that works consistently across all platforms - from ASP.NET Core backend to native mobile apps.

**Location**: [`/src/Shared/Services/AppEnvironment.cs`](/src/Shared/Services/AppEnvironment.cs)

**Why This Matters**:
Unlike ASP.NET Core which uses environment variables (that can be set at runtime), Android, iOS, Windows, and macOS don't support the exact same concept. `AppEnvironment` provides a unified abstraction that works everywhere.

**Key Code**:
```csharp
public static partial class AppEnvironment
{
    public static string Current { get; private set; } =
#if Development            // dotnet publish -c Debug
        Development;
#elif Test                 // dotnet publish -c Release -p:Environment=Test
        Test;
#elif Staging              // dotnet publish -c Release -p:Environment=Staging
        Staging;
#else                      // dotnet publish -c Release
        Production;
#endif

    public static bool IsDevelopment() => Is(Development);
    public static bool IsTest() => Is(Test);
    public static bool IsStaging() => Is(Staging);
    public static bool IsProduction() => Is(Production);
}
```

**Key Benefits**:
1. **Cross-Platform Consistency**: Works identically across server, web, Android, iOS, Windows, and macOS
2. **Build-Time Configuration**: Environment is determined at build/publish time using MSBuild properties
3. **Compile-Time Constants**: Uses C# preprocessor directives for zero runtime overhead
4. **Type-Safe**: IntelliSense support and compile-time checking

**Usage Examples**:
```csharp
// Check current environment in any C# file
if (AppEnvironment.IsDevelopment())
{
    // Development-specific code
}

// Get environment name
string env = AppEnvironment.Current; // "Development", "Production", etc.

// Set environment (done automatically at startup for server projects)
AppEnvironment.Set(builder.Environment.EnvironmentName);
```

**Razor Component Usage**:
```xml
@if (AppEnvironment.IsDevelopment())
{
    <BitMessageBar MessageBarType="BitMessageBarType.Warning">
        Running in Development mode
    </BitMessageBar>
}
```

### MSBuild Environment Configuration

**Location**: [`/src/Directory.Build.props`](/src/Directory.Build.props)

The build system automatically configures the environment based on build configuration:

```xml
<!-- Default environment mapping -->
<Environment Condition="'$(Environment)' == '' AND '$(Configuration)' == 'Release'">Production</Environment>
<Environment Condition="'$(Environment)' == '' AND $(Configuration.Contains('Debug'))">Development</Environment>

<!-- Environment becomes a compile-time constant -->
<DefineConstants>$(DefineConstants);$(Environment);$(Configuration)</DefineConstants>
```

**What This Means**:
- When you build with `-c Debug`, the `Development` preprocessor constant is automatically defined
- When you build with `-c Release`, the `Production` constant is defined by default
- You can override with `-p:Environment=Staging` or `-p:Environment=Test`

**Key Benefits**:

1. **Environment Information Available Everywhere**: You can access environment information in:
   - **C# code** (via `AppEnvironment.Current`)
   - **MSBuild scripts** (via `$(Environment)` property)
   - **Razor components** (via `@AppEnvironment.Current`)

2. **Environment-Specific Code**: Write conditional code that compiles differently per environment:
```csharp
#if Development
    // This code only exists in Development builds
    services.AddDeveloperTools();
#endif

#if Production
    // Production-only optimization
    services.AddResponseCompression();
#endif
```

3. **Platform Detection**: Similar constants for platform-specific code:
```csharp
#if Android
    var dataPath = FileSystem.AppDataDirectory;
#elif iOS
    var dataPath = NSFileManager.DefaultManager.GetUrls(
        NSSearchPathDirectory.DocumentDirectory, 
        NSSearchPathDomain.User)[0].Path;
#elif Windows
    var dataPath = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData);
#endif
```

---

## 🔄 CI/CD Workflow Overview

The project includes a complete CI/CD pipeline setup using GitHub Actions with **4 workflow files**:

### 1. Continuous Integration (CI) - `ci.yml`

**File**: [`/.github/workflows/ci.yml`](/.github/workflows/ci.yml)

**Triggers**:
- Pull requests to any branch
- Manual workflow dispatch

**What It Does**:
```
✓ Checks out code
✓ Sets up .NET SDK (from global.json) and Node.js 23
✓ Restores workloads (dotnet workload restore)
✓ Builds entire solution (Boilerplate.slnx)
✓ Installs Playwright browsers with dependencies
✓ Runs all tests (unit + integration + UI tests)
✓ Uploads test results as artifacts if tests fail
```

**Key Configuration**:
- **Runner**: Ubuntu 24.04
- **SDK Version**: Automatically detected from `global.json`
- **Node Version**: 23
- **Test Artifacts**: Retained for 14 days on failure

**Important**: The CI workflow ensures that all code changes are validated before merging. It's the gatekeeper for code quality.

---

### 2. Production Deployment - `cd-production.yml`

**File**: [`/.github/workflows/cd-production.yml`](/.github/workflows/cd-production.yml)

**Triggers**:
- Push to `main` branch (automatic)
- Manual workflow dispatch

**What It Does**:
```yaml
calls: cd-template.yml
with:
  ENV_NAME: "Production"
secrets: inherit
```

This is a lightweight wrapper that triggers the reusable deployment template with the Production environment configuration.

---

### 3. Test Environment Deployment - `cd-test.yml`

**File**: [`/.github/workflows/cd-test.yml`](/.github/workflows/cd-test.yml)

**Triggers**:
- Push to `test` branch (automatic)
- Manual workflow dispatch

**What It Does**:
```yaml
calls: cd-template.yml
with:
  ENV_NAME: "Test"
secrets: inherit
```

This triggers the same reusable template but with Test environment configuration.

**Pattern**: You can create similar files for Staging or any other environment you need (e.g., `cd-staging.yml`).

---

### 4. Reusable Deployment Template - `cd-template.yml`

**File**: [`/.github/workflows/cd-template.yml`](/.github/workflows/cd-template.yml)

This is the **core deployment workflow** that handles building and deploying all platforms. It's a reusable workflow called by production, test (and any other environment-specific workflows you create).

**Key Features**:
- **Environment-Agnostic**: Accepts `ENV_NAME` parameter (Production, Test, Staging, etc.)
- **Multi-Platform**: Builds server backend, Blazor WebAssembly, Android, iOS, macOS, and Windows
- **Parallel Jobs**: All platform builds run in parallel for speed
- **Artifact Storage**: Each platform produces an artifact that can be deployed independently

**Jobs Overview**:
1. **build_api_blazor** → **deploy_api_blazor**: Server backend + Blazor WebAssembly
2. **build_blazor_hybrid_windows**: Windows desktop app (.exe installer)
3. **build_blazor_hybrid_android**: Android app bundle (.aab for Google Play)
4. **build_blazor_hybrid_iOS**: iOS app package (.ipa for App Store) and macOS app

---

## 🏗️ Detailed Build and Deployment Pipeline

### Job 1: Build API + Blazor WebAssembly

**Platform**: Ubuntu 24.04  
**Environment**: Uses GitHub environment variables and secrets

**Step-by-Step Process**:

1. **Environment Setup**
   ```yaml
   - Checkout source code
   - Setup .NET SDK (from global.json)
   - Setup Node.js 23
   ```

2. **Localization with Bit.ResxTranslator**
   ```bash
   dotnet tool install --global Bit.ResxTranslator
   bit-resx-translate
   ```
   - Automatically translates all `.resx` resource files
   - Ensures multi-language support is up to date

3. **Configuration Substitution**
   ```yaml
   - Updates appsettings*.json files with environment variables
   - ServerAddress: Points to environment-specific API URL
   - BlazorMode: Set to 'BlazorWebAssembly'
   - VAPID keys: For web push notifications
   ```

4. **Build Process**
   ```bash
   # Install WebAssembly tools
   dotnet workload install wasm-tools
   
   # Generate CSS/JS from TypeScript and SCSS
   dotnet build -t:BeforeBuildTasks -c Release -p:Version="1.0.0"
   
   # Publish self-contained Linux binary
   dotnet publish -c Release --self-contained -r linux-x64 \
     -p:Version="1.0.0" -p:Environment=Production
   ```

5. **Artifact Upload**
   ```yaml
   - Upload server bundle (includes all files)
   - include-hidden-files: true  # Critical for .well-known folder
   ```
   - The `.well-known` folder is required for WebAuthn and ACME challenges

**Key Configuration Values**:
- **BlazorMode**: `BlazorWebAssembly` (for production deployments)
- **Self-Contained**: `true` (includes .NET runtime in output)
- **Runtime Identifier**: `linux-x64` (for Linux servers)
- **Environment**: Injected via `-p:Environment=${{ inputs.ENV_NAME }}`

---

### Job 2: Deploy API + Blazor WebAssembly

**Depends On**: Job 1 (Build API + Blazor WebAssembly)  
**Platform**: Ubuntu 24.04

**Step-by-Step Process**:

1. **Download Build Artifact**
   ```yaml
   - Download server-bundle from previous job
   - Contains complete self-contained application
   ```

2. **Azure Web App Deployment**
   ```yaml
   - Deploy to Azure Web App (optional - you can use any hosting)
   - Uses publish profile from secrets
   - Deploys to production slot
   - Returns webapp URL
   ```

3. **CDN Cache Purge**
   ```yaml
   - Purge Cloudflare cache (if configured)
   - Ensures users get the latest version immediately
   - No stale cached responses
   ```

**Important Notes**:
- ✅ **Azure is Optional**: You can deploy to AWS, Google Cloud, your own servers, Docker, Kubernetes, etc.
- ✅ **Cloudflare is Optional**: CDN cache purging is only needed if you're using a CDN
- ⚠️ **Not Yet Aspire-Friendly**: The backend deployment workflow doesn't yet integrate with .NET Aspire orchestration

---

### Job 3: Build Windows Desktop App

**Platform**: Windows 2025  
**Environment**: Uses GitHub environment variables and secrets

**Step-by-Step Process**:

1. **Environment Setup & Configuration**
   ```yaml
   - Setup .NET SDK and Node.js
   - Translate resource files (bit-resx-translate)
   - Update appsettings.json:
     - ServerAddress: Environment-specific API URL
     - WindowsUpdate.FilesUrl: Auto-update endpoint
   ```

2. **Build & Package with Velopack**
   ```bash
   # Generate CSS/JS files
   dotnet build -t:BeforeBuildTasks -c Release
   
   # Publish for Windows x86 (32-bit for wider compatibility)
   dotnet publish -c Release -r win-x86 --self-contained \
     -p:Version="1.0.0" -p:Environment=Production
   
   # Create installer with Velopack
   dotnet vpk pack \
     -u com.company.app \           # Application ID
     -v 1.0.0 \                     # Version
     -p .\publish-result \          # Published files location
     -e Boilerplate.Client.Windows.exe \  # Main executable
     -r win-x86 \                   # Runtime
     --framework webview2 \         # Include WebView2 runtime
     --icon .\wwwroot\favicon.ico \ # App icon
     --packTitle 'My App'           # Display name
   ```

3. **Upload Artifact**
   ```yaml
   - Upload Windows installer (.exe) to artifacts
   - Located in Releases folder
   ```

**Velopack Features**:
- **Auto-Update Support**: Built-in automatic update mechanism
- **WebView2 Runtime**: Packages Microsoft Edge WebView2 with the installer
- **Delta Updates**: Only downloads changed files for updates
- **Version Management**: Handles version tracking automatically
- **x86 Build**: 32-bit build runs on both 32-bit and 64-bit Windows

---

### Job 4: Build Android App

**Platform**: Ubuntu 24.04  
**Environment**: Uses GitHub environment variables and secrets

**Step-by-Step Process**:

1. **Setup Android Signing**
   ```yaml
   - Extract Android signing key from base64-encoded secret
   - Save as Boilerplate.keystore in project directory
   - Configure keystore for release signing
   ```
   - The keystore is stored as a GitHub secret in base64 format for security

2. **Build Android App Bundle (AAB)**
   ```bash
   # Install MAUI Android workload
   dotnet workload install maui-android
   
   # Install Android SDK platform tools
   ${ANDROID_SDK_ROOT}/cmdline-tools/latest/bin/sdkmanager \
     --sdk_root=$ANDROID_SDK_ROOT "platform-tools"
   
   # Generate CSS/JS files
   dotnet build -t:BeforeBuildTasks -c Release
   
   # Publish signed AAB
   dotnet publish -c Release \
     -p:ApplicationId=com.company.app \
     -p:AndroidPackageFormat=aab \
     -p:AndroidKeyStore=true \
     -p:AndroidSigningKeyStore="Boilerplate.keystore" \
     -p:AndroidSigningKeyAlias=Boilerplate \
     -p:AndroidSigningKeyPass="password" \
     -p:AndroidSigningStorePass="password" \
     -p:Version="1.0.0" \
     -p:Environment=Production \
     -f net10.0-android
   ```

3. **Upload Artifact**
   ```yaml
   - Upload signed .aab file
   - Ready for Google Play Store submission
   ```

**Android Signing Configuration**:
- **Package Format**: AAB (Android App Bundle) - required for Play Store
- **Keystore**: Same signing key must be used for all app updates
- **Alias**: Identifier for the key within the keystore
- **Passwords**: Keystore password and key password (stored as secrets)

**Important**: The signing key is **critical**. If you lose it, you cannot update your app on Google Play Store!

---

### Job 5: Build iOS and macOS Apps

**Platform**: macOS 15  
**Environment**: Uses GitHub environment variables and secrets

**Step-by-Step Process**:

1. **Setup Apple Development Environment**
   ```yaml
   - Setup .NET SDK
   - Setup Xcode 26.0 (latest)
   - Setup Node.js 23
   - Translate resources (bit-resx-translate)
   - Update appsettings.json with ServerAddress
   ```

2. **Apple Code Signing Setup**
   ```yaml
   # Import distribution certificate
   - Import P12 certificate from base64 secret
   - Certificate password from secrets
   
   # Download provisioning profile
   - Uses App Store Connect API
   - Requires: Issuer ID, API Key ID, Private Key
   - Downloads profile for specified Bundle ID
   ```

3. **Build iOS App Package (IPA)**
   ```bash
   # Install MAUI workload (includes iOS support)
   dotnet workload install maui
   
   # Generate CSS/JS files
   dotnet build -t:BeforeBuildTasks -c Release
   
   # Publish and sign IPA
   dotnet publish \
     -p:ApplicationId=com.company.app \
     -p:RuntimeIdentifier=ios-arm64 \     # For physical devices
     -c Release \
     -p:ArchiveOnBuild=true \             # Create .ipa
     -p:CodesignKey="iPhone Distribution" \  # Certificate name
     -p:CodesignProvision="MyApp Provisioning" \  # Profile name
     -p:Version="1.0.0" \
     -p:Environment=Production \
     -f net10.0-ios
   ```

4. **Upload Artifact**
   ```yaml
   - Upload signed .ipa file
   - Ready for App Store submission via App Store Connect
   ```

**Apple Requirements**:
- **Distribution Certificate**: Required to sign apps for App Store
- **Provisioning Profile**: Links your app ID, certificate, and devices
- **App Store Connect API**: Automates profile download
- **Bundle ID**: Must match your app's identifier in App Store Connect
- **Xcode**: Required for iOS/macOS builds (only available on macOS)

**iOS Build Notes**:
- **Runtime Identifier**: `ios-arm64` for physical iOS devices (iPhone, iPad)
- **Signing**: Both certificate and provisioning profile must be valid and matching
- **Same Job for macOS**: The workflow can be extended to build macOS apps using similar steps

---

## 🎯 Two-Phase Deployment Architecture (Best Practice)

The workflow follows a **security-focused two-phase deployment** pattern that separates building from deploying:

### Phase 1: Build
**Purpose**: Compile, bundle, and package the application  
**Runner Type**: Feature-rich build agents

**Characteristics**:
- Has full SDK installations (.NET, Node.js, Android SDK, Xcode)
- Performs compilation, transpilation, bundling
- Runs tests and quality checks
- Uploads artifacts to GitHub (or Azure DevOps)
- **No production access** - isolated from production systems

### Phase 2: Deploy
**Purpose**: Take pre-built artifacts and deploy them  
**Runner Type**: Lightweight deployment agents

**Characteristics**:
- No SDKs required - only deployment tools
- **More Secure**: Limited tooling, minimal attack surface
- **Direct production access**: Can connect to production servers
- Downloads pre-built artifacts and deploys them
- No compilation or build processes

### Why This Separation Matters

```
┌─────────────────────────────┐         ┌─────────────────────────────┐
│    Build Agent (Heavy)      │         │   Deploy Agent (Light)      │
├─────────────────────────────┤         ├─────────────────────────────┤
│ ✓ .NET SDK                  │         │ ✗ No SDKs                   │
│ ✓ Node.js & npm             │         │ ✓ Deployment tools only     │
│ ✓ Android SDK               │         │ ✓ Direct production access  │
│ ✓ Xcode (for iOS/macOS)     │         │ ✓ Minimal attack surface    │
│ ✓ Build tools & compilers   │         │ ✓ Security-hardened         │
│ ✗ No production access      │         │ ✓ Can deploy to servers     │
└─────────────────────────────┘         └─────────────────────────────┘
```

**Security Benefits**:
1. **Principle of Least Privilege**: The agent with production access has minimal software installed
2. **Reduced Attack Surface**: Fewer tools = fewer potential vulnerabilities
3. **Separation of Concerns**: Build failures don't affect production; deployment issues don't corrupt build artifacts
4. **Audit Trail**: Clear separation between what was built and what was deployed

**Example from the Workflow**:
```yaml
# Phase 1: Build (no production access)
build_api_blazor:
  runs-on: ubuntu-24.04
  steps:
    - uses: actions/checkout@v5
    - uses: actions/setup-dotnet@v5
    - uses: actions/setup-node@v6
    - run: dotnet publish ...
    - uses: actions/upload-artifact@v5  # Save artifact

# Phase 2: Deploy (has production access)
deploy_api_blazor:
  needs: build_api_blazor  # Depends on Phase 1
  runs-on: ubuntu-24.04
  steps:
    - uses: actions/download-artifact@v5  # Get pre-built artifact
    - uses: azure/webapps-deploy@v3      # Deploy to production
```

**Result**: Even if an attacker compromises the build agent (which has many tools installed), they cannot access production. Conversely, if the deployment agent is compromised, the attacker cannot inject malicious code into the build process.

---

## 🔐 Required Secrets and Variables

The CI/CD pipeline requires various secrets and variables to be configured in GitHub. These are stored per environment (Production, Test, Staging, etc.).

### How to Configure in GitHub

1. Navigate to your repository → **Settings** → **Environments**
2. Create environment (e.g., "Production", "Test")
3. Add **Secrets** (encrypted, never exposed) and **Variables** (plain text, visible)

---

### GitHub Secrets (Encrypted)

#### Backend Deployment
| Secret Name | Purpose | Example/Notes |
|------------|---------|---------------|
| `AZURE_PUBLISH_PROFILE` | Azure Web App publish profile | Download from Azure Portal (optional) |
| `CLOUDFLARE_ZONE` | Cloudflare zone ID for cache purge | Found in Cloudflare dashboard (optional) |
| `CLOUDFLARE_TOKEN` | Cloudflare API token | Create in Cloudflare with Cache Purge permission (optional) |

#### App Configuration
| Secret Name | Purpose | Example/Notes |
|------------|---------|---------------|
| `PUBLIC_VAPIDKEY` | Web push notifications public VAPID key | Generate using `npx web-push generate-vapid-keys` |

#### Android Signing
| Secret Name | Purpose | Example/Notes |
|------------|---------|---------------|
| `ANDROID_RELEASE_KEYSTORE_FILE_BASE64` | Base64-encoded Android keystore | `base64 -i Boilerplate.keystore` |
| `ANDROID_RELEASE_KEYSTORE_PASSWORD` | Keystore password | Password you set when creating keystore |
| `ANDROID_RELEASE_SIGNING_PASSWORD` | Key password within keystore | Password for the specific key alias |

**Important**: Keep your Android keystore safe! If you lose it, you cannot update your app on Google Play Store.

#### iOS & macOS Signing
| Secret Name | Purpose | Example/Notes |
|------------|---------|---------------|
| `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64` | Base64-encoded P12 certificate | `base64 -i Certificates.p12` |
| `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD` | P12 certificate password | Password you set when exporting certificate |
| `APPSTORE_API_KEY_ISSUER_ID` | App Store Connect API issuer ID | Found in App Store Connect → Users and Access → Keys |
| `APPSTORE_API_KEY_ID` | App Store Connect API key ID | Created in App Store Connect → Users and Access → Keys |
| `APPSTORE_API_KEY_PRIVATE_KEY` | App Store Connect private key | Downloaded when creating API key (only once!) |

---

### GitHub Variables (Plain Text)

| Variable Name | Purpose | Example |
|--------------|---------|---------|
| `APP_VERSION` | Application version number | `1.0.0` or `2.1.5` |
| `APP_ID` | Bundle/package identifier | `com.company.myapp` |
| `APP_TITLE` | Application display name | `My Application` |
| `SERVER_ADDRESS` | API server URL | `https://api.myapp.com` or `https://test-api.myapp.com` |
| `WINDOWS_UPDATE_FILES_URL` | Windows auto-update endpoint | `https://myapp.com/downloads/windows` |
| `APP_SERVICE_NAME` | Azure App Service name | `myapp-prod` (only if using Azure) |
| `IOS_CODE_SIGN_PROVISION` | iOS provisioning profile name | `MyApp Distribution Profile` |

---

### Environment-Specific Configuration Example

**Production Environment**:
```
Variables:
  SERVER_ADDRESS = https://api.myapp.com
  APP_VERSION = 1.0.0
  APP_TITLE = My App

Secrets:
  AZURE_PUBLISH_PROFILE = <production publish profile>
  PUBLIC_VAPIDKEY = <production VAPID key>
```

**Test Environment**:
```
Variables:
  SERVER_ADDRESS = https://test-api.myapp.com
  APP_VERSION = 1.0.0-test
  APP_TITLE = My App (Test)

Secrets:
  AZURE_PUBLISH_PROFILE = <test publish profile>
  PUBLIC_VAPIDKEY = <test VAPID key>
```

This allows the same workflow to deploy different configurations to different environments.

---

## 📝 Build-Time Configuration Substitution

During the CI/CD process, the workflow modifies `appsettings.json` files **before building** the application. This allows environment-specific configuration without maintaining multiple configuration files.

### How It Works

The workflow uses the `variable-substitution` action to replace values in JSON files:

```yaml
- name: Update core appsettings.json
  uses: devops-actions/variable-substitution@v1.2 
  with:
    files: 'src/**/appsettings*json'  # Matches all appsettings files
  env:
    ServerAddress: ${{ vars.SERVER_ADDRESS }}
    WebAppRender.BlazorMode: 'BlazorWebAssembly'
    AdsPushVapid.PublicKey: ${{ secrets.PUBLIC_VAPIDKEY }}
```

### What Gets Replaced

**Before** (in repository):
```json
{
  "ServerAddress": "https://localhost:5001",
  "WebAppRender": {
    "BlazorMode": "BlazorServer"
  },
  "AdsPushVapid": {
    "PublicKey": "dev-key"
  }
}
```

**After** (during build):
```json
{
  "ServerAddress": "https://api.myapp.com",
  "WebAppRender": {
    "BlazorMode": "BlazorWebAssembly"
  },
  "AdsPushVapid": {
    "PublicKey": "production-vapid-key"
  }
}
```

### Why This Matters

**Benefits**:
1. **Single Source of Truth**: One set of appsettings files in repository
2. **No Secrets in Code**: Production values stored in GitHub Secrets/Variables
3. **Environment-Specific**: Same workflow produces different builds for different environments
4. **No Manual Editing**: Automated substitution eliminates human error

**Common Substitutions**:
- `ServerAddress`: Points to the correct backend API for each environment
- `BlazorMode`: Typically set to `BlazorWebAssembly` for production (better performance)
- `VAPID Keys`: Different push notification keys per environment
- `WindowsUpdate.FilesUrl`: Different update servers per environment
- `ApplicationInsights.ConnectionString`: Different monitoring per environment

**Platform-Specific Files**:
```yaml
# For Linux/Unix (server, Android, iOS)
files: 'src/**/appsettings*json'

# For Windows
files: 'src\**\appsettings*json'
```

The glob pattern matches:
- `src/Shared/appsettings.json`
- `src/Client/Boilerplate.Client.Core/appsettings.json`
- `src/Client/Boilerplate.Client.Maui/appsettings.Production.json`
- All other `appsettings*.json` files in the project

---

## 🚀 Platform Support Matrix

| Platform | Build Runner | Artifact Type | Final Output | Distribution Channel |
|----------|--------------|---------------|--------------|---------------------|
| **Server API + Blazor** | Ubuntu 24.04 | Self-contained Linux binary | `.dll` + runtime | Azure Web App, AWS, GCP, Docker, Kubernetes, Linux servers |
| **Windows Desktop** | Windows 2025 | Velopack installer | `.exe` installer | Direct download, auto-update server |
| **Android** | Ubuntu 24.04 | Android App Bundle | `.aab` (signed) | Google Play Store |
| **iOS** | macOS 15 | iOS App Package | `.ipa` (signed) | Apple App Store (via App Store Connect) |
| **macOS** | macOS 15 | macOS App Package | `.app` or `.pkg` | Apple App Store, direct download |

### Platform Details

#### Server API + Blazor WebAssembly
- **Build**: Self-contained includes .NET runtime (no server-side installation needed)
- **Runtime**: `linux-x64` (most common for servers)
- **Size**: Larger due to included runtime, but more portable
- **Deployment**: Can run on any Linux server without .NET SDK installed

#### Windows Desktop
- **Architecture**: `win-x86` (32-bit) - runs on both 32-bit and 64-bit Windows
- **Auto-Update**: Built-in using Velopack
- **WebView2**: Packaged with installer for offline installation
- **Compatibility**: Windows 7, 8, 10, 11 (wider reach than MAUI Windows which requires 10+)

#### Android
- **Format**: AAB (Android App Bundle) - required by Google Play Store since August 2021
- **Signing**: Must be signed with same keystore for all updates
- **Target SDK**: Configured in project file, must meet Google Play requirements
- **Minimum SDK**: API 26 (Android 8.0) - configurable in `Directory.Build.props`

#### iOS
- **Architecture**: `ios-arm64` - for physical devices (iPhone, iPad with A7 chip or later)
- **Simulator**: Requires separate build with `iossimulator-x64` or `iossimulator-arm64`
- **Signing**: Requires distribution certificate + provisioning profile
- **Submission**: Via App Store Connect, requires Apple Developer Program membership

#### macOS
- **Same Build Process**: Built alongside iOS in the same job
- **Runtime**: `osx-x64` or `osx-arm64` (Apple Silicon)
- **Distribution**: App Store or direct download (requires notarization for direct)

---

## ⚠️ Important Notes and Limitations

### Backend CI/CD - Not Yet Aspire-Friendly

**Current State**:
- ✅ **Client platforms** (Android, iOS, Windows, macOS) have complete CI/CD support
- ✅ **Backend deployment works** but is demonstrated using Azure Web Apps
- ⚠️ **Not optimized for .NET Aspire** orchestration yet

**What This Means for You**:
1. **Azure is Optional**: The workflow shows Azure deployment as an example
   - You can deploy to **AWS**, **Google Cloud**, **DigitalOcean**, **your own servers**, etc.
   - Modify the `deploy_api_blazor` job to use your preferred hosting provider
   
2. **Aspire Deployment Pending**: 
   - If you're using .NET Aspire for local development (with AppHost), deployment workflows don't yet integrate with Aspire's deployment features
   - You'll need to adapt the deployment for Aspire-based hosting
   - The current approach publishes a standalone application, not an Aspire-orchestrated deployment

3. **Backend CD Demonstrates Best Practices**:
   - Even though Aspire integration is pending, the workflow demonstrates the **two-phase deployment** pattern
   - This security-focused approach is still valuable regardless of your hosting choice

**Workaround Options**:
- **Option 1**: Deploy backend separately without Aspire orchestration (current approach)
- **Option 2**: Deploy to Azure Container Apps using Aspire's built-in support
- **Option 3**: Generate Kubernetes manifests from Aspire and deploy to K8s cluster
- **Option 4**: Use Docker Compose generated from Aspire for simpler container deployments

### Customizing for Your Infrastructure

The provided workflows are **templates** - they're designed to be adapted:

**You Can**:
- ✅ Use different hosting providers (AWS, GCP, Heroku, Vercel, etc.)
- ✅ Add additional environments (Staging, QA, UAT)
- ✅ Integrate with other CI/CD platforms (Azure DevOps, GitLab CI, Jenkins, CircleCI)
- ✅ Modify build configurations (different runtime identifiers, build modes)
- ✅ Add additional steps (security scanning, performance testing, etc.)
- ✅ Change deployment strategies (blue-green, canary, rolling updates)

**You Should**:
- 🔧 Replace Azure deployment with your chosen hosting provider
- 🔧 Update secrets and variables for your infrastructure
- 🔧 Adapt environment names to match your organization's conventions
- 🔧 Add approval gates for production deployments (GitHub Environments support this)

**Example: Switching from Azure to AWS**:
```yaml
# Replace this Azure deployment step:
- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ vars.APP_SERVICE_NAME }}
    publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}

# With AWS Elastic Beanstalk deployment:
- name: Deploy to AWS Elastic Beanstalk
  uses: einaregilsson/beanstalk-deploy@v21
  with:
    aws_access_key: ${{ secrets.AWS_ACCESS_KEY_ID }}
    aws_secret_key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    application_name: my-app
    environment_name: my-app-prod
    version_label: ${{ github.sha }}
    region: us-east-1
    deployment_package: server.zip
```

---

## 🎓 Key Takeaways

### 1. Unified Environment System
**AppEnvironment** provides consistent environment detection across **all platforms**:
- ✅ Server (ASP.NET Core)
- ✅ Blazor WebAssembly (browser)
- ✅ Android (mobile)
- ✅ iOS (mobile)
- ✅ macOS (desktop)
- ✅ Windows (desktop - both MAUI and Windows Forms)

Unlike traditional environment variables (which don't work consistently across platforms), `AppEnvironment` uses compile-time constants that work everywhere.

### 2. Build-Time Environment Configuration
Environments are set **at build time**, not runtime:
```bash
# Development build
dotnet publish -c Debug

# Production build
dotnet publish -c Release

# Custom environment
dotnet publish -c Release -p:Environment=Staging
```

This creates **optimized, environment-specific builds** with:
- Dead code elimination (unused environment code removed)
- Environment-specific configuration baked in
- Zero runtime overhead for environment checks

### 3. Multi-Platform CI/CD in One Workflow
A **single reusable workflow template** (`cd-template.yml`) handles:
- Server backend (Linux)
- Blazor WebAssembly (browser)
- Android apps (.aab)
- iOS apps (.ipa)
- Windows desktop apps (.exe)

All platforms build **in parallel** for speed, then deploy independently.

### 4. Security-Focused Two-Phase Deployment
The workflow implements **security best practices**:
1. **Build Phase**: Feature-rich agent (SDKs, tools) with **no production access**
2. **Deploy Phase**: Lightweight agent (minimal tools) with **production access**

This limits the attack surface of production-connected systems.

### 5. Environment-Aware Configuration System
The multi-layered configuration system supports:
```
Priority (Low → High):
1. appsettings.json (base settings)
2. appsettings.{Environment}.json (environment overrides)
3. Build-time substitution (CI/CD secrets/variables)
4. Runtime environment variables (server only)
```

This allows the same codebase to run in different environments with different configurations.

### 6. Complete Automation
From **code commit** to **app store submission**, the pipeline automates:
- ✅ Building for all platforms
- ✅ Running tests
- ✅ Signing apps (Android, iOS)
- ✅ Deploying backend
- ✅ Creating installers (Windows)
- ✅ Generating store-ready artifacts (.aab, .ipa)
- ✅ Purging CDN caches

### 7. Environment Awareness Everywhere
You can write **environment-specific code** in:

**C# Code**:
```csharp
if (AppEnvironment.IsProduction())
{
    // Production logic
}
```

**Razor Components**:
```xml
@if (AppEnvironment.IsDevelopment())
{
    <DebugToolbar />
}
```

**MSBuild Scripts** (`.csproj` files):
```xml
<PropertyGroup Condition="'$(Environment)' == 'Production'">
  <EnableOptimizations>true</EnableOptimizations>
</PropertyGroup>
```

**Compile-Time Conditional Compilation**:
```csharp
#if Development
    services.AddDetailedErrorPages();
#endif
```

---

## 🔗 Real-World Usage Examples

### Example 1: Adding a New "Staging" Environment

**Step 1**: Update `AppEnvironment.cs` (already supports Staging)

**Step 2**: Create `appsettings.Staging.json` files where needed:
```json
{
  "ServerAddress": "https://staging-api.myapp.com",
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**Step 3**: Add GitHub Environment named "Staging" with variables/secrets:
```
Variables:
  SERVER_ADDRESS = https://staging-api.myapp.com
  APP_VERSION = 1.0.0-staging
  APP_TITLE = My App (Staging)

Secrets:
  AZURE_PUBLISH_PROFILE = <staging publish profile>
  PUBLIC_VAPIDKEY = <staging VAPID key>
```

**Step 4**: Create workflow file `cd-staging.yml`:
```yaml
name: Boilerplate Staging CD
on:
  push:
    branches: [ "staging" ]
  workflow_dispatch:

jobs:
  build_and_deploy_staging:
    uses: ./.github/workflows/cd-template.yml
    with:
      ENV_NAME: Staging
    secrets: inherit
```

**Step 5**: Build for staging locally:
```bash
dotnet publish -c Release -p:Environment=Staging
```

---

### Example 2: Checking Environment in Code

**In a Controller**:
```csharp
public class ConfigController : AppControllerBase
{
    [HttpGet]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            Environment = AppEnvironment.Current,
            DetailedErrors = AppEnvironment.IsDevelopment(),
            Features = new
            {
                EnableDebugMode = AppEnvironment.IsDevelopment(),
                EnableAnalytics = AppEnvironment.IsProduction()
            }
        });
    }
}
```

**In a Blazor Component**:
```xml
@page "/debug-info"

<h3>Debug Information</h3>

@if (AppEnvironment.IsDevelopment())
{
    <BitMessageBar MessageBarType="BitMessageBarType.Warning">
        Running in Development mode - Detailed errors enabled
    </BitMessageBar>
    
    <ul>
        <li>Environment: @AppEnvironment.Current</li>
        <li>Build: Debug</li>
        <li>Platform: @GetPlatform()</li>
    </ul>
}
else if (AppEnvironment.IsProduction())
{
    <BitMessageBar MessageBarType="BitMessageBarType.Info">
        Production Environment
    </BitMessageBar>
}

@code {
    private string GetPlatform()
    {
#if Android
        return "Android";
#elif iOS
        return "iOS";
#elif Windows
        return "Windows";
#elif Mac
        return "macOS";
#else
        return "Web";
#endif
    }
}
```

**Platform-Specific Code with Environment Check**:
```csharp
public async Task<string> GetLocalStoragePath()
{
    string basePath;
    
#if Android
    basePath = FileSystem.AppDataDirectory;
#elif iOS
    basePath = NSFileManager.DefaultManager
        .GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)[0].Path;
#elif Windows
    basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#else
    basePath = "browser-storage"; // Web version uses IndexedDB
#endif

    // Add environment-specific subfolder
    if (AppEnvironment.IsDevelopment())
    {
        basePath = Path.Combine(basePath, "dev-data");
    }
    else if (AppEnvironment.IsTest())
    {
        basePath = Path.Combine(basePath, "test-data");
    }
    
    return basePath;
}
```

---

### Example 3: Environment-Specific Service Registration

```csharp
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Always register core services
        services.AddScoped<IDataService, DataService>();
        
#if Development
        // Development-only services
        services.AddScoped<IDebugService, DebugService>();
        services.AddDatabaseDeveloperPageExceptionFilter();
#endif

#if Production
        // Production-only optimizations
        services.AddResponseCompression();
        services.AddResponseCaching();
#endif

        // Runtime environment checks for services that need runtime config
        if (AppEnvironment.IsProduction())
        {
            services.AddApplicationInsightsTelemetry();
        }
        else if (AppEnvironment.IsDevelopment())
        {
            services.AddDeveloperExceptionPage();
        }
        
        return services;
    }
}
```

---

### Example 4: Conditional MSBuild Properties

In your `.csproj` file:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <!-- Development-specific settings -->
  <PropertyGroup Condition="'$(Environment)' == 'Development'">
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <WarningsAsErrors></WarningsAsErrors>
    <Optimize>false</Optimize>
  </PropertyGroup>

  <!-- Production-specific settings -->
  <PropertyGroup Condition="'$(Environment)' == 'Production'">
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <WarningsAsErrors>CS8600;CS8602;CS8603</WarningsAsErrors>
    <Optimize>true</Optimize>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>

</Project>
```

---

## 📚 Additional Resources

- **GitHub Actions Documentation**: https://docs.github.com/en/actions
- **GitHub Environments**: https://docs.github.com/en/actions/deployment/targeting-different-environments
- **.NET Publishing**: https://learn.microsoft.com/en-us/dotnet/core/deploying/
- **Android App Signing**: https://developer.android.com/studio/publish/app-signing
- **iOS Code Signing**: https://developer.apple.com/support/code-signing/
- **Velopack (Windows Updates)**: https://github.com/velopack/velopack
- **Bit.ResxTranslator**: https://github.com/bitfoundation/bitplatform/tree/develop/src/ResxTranslator

---

## 🎯 Next Steps

Now that you understand the CI/CD pipeline and environment configuration:

1. **Set up your GitHub environments** (Production, Test, etc.)
2. **Configure required secrets and variables** in GitHub
3. **Test the CI workflow** by creating a pull request
4. **Customize deployment jobs** for your hosting infrastructure
5. **Add approval gates** for production deployments (recommended)
6. **Monitor your deployments** using GitHub Actions logs

**Remember**: These workflows are templates. Adapt them to your infrastructure, team workflow, and deployment requirements!

---