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
✓ Sets up .NET SDK (from global.json) and Node.js 24
✓ Restores workloads (dotnet workload restore)
✓ Builds entire solution (Boilerplate.slnx)
✓ Installs Playwright browsers with dependencies
✓ Runs all tests (unit + integration + UI tests)
✓ Uploads test results as artifacts if tests fail
```

**Key Configuration**:
- **Runner**: Ubuntu 24.04
- **SDK Version**: Automatically detected from `global.json`
- **Node Version**: 24
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

**Step-by-Step Process**:

1. **Environment Setup**
   ```yaml
   - Checkout source code
   - Setup .NET SDK (from global.json)
   - Setup Node.js 24
   ```

2. **Localization with Bit.ResxTranslator**
   ```bash
   dotnet tool install --global Bit.ResxTranslator
   bit-resx-translate
   ```
   - Automatically translates all `.resx` resource files missing values

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
   
   # Publish self-contained Linux binary (Using Linux is optional)
   dotnet publish -c Release --self-contained -r linux-x64 \
     -p:Version="1.0.0" -p:Environment=Production
   ```

5. **Artifact Upload**

---

### Job 2: Deploy API + Blazor WebAssembly

**Depends On**: Job `Build API + Blazor WebAssembly` job

**Platform**: Ubuntu 24.04

**Step-by-Step Process**:

1. **Download Build Artifact**
   ```yaml
   - Download server-bundle from previous job
   - Contains complete self-contained application
   ```

2. **Azure Web App Deployment** (optional - you can use any hosting)
   ```yaml
   - Deploy to Azure Web App
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
- ✅ **Azure is Optional**: You can deploy to AWS, Google Cloud, your own servers, Docker, Kubernetes, Windows/IIS etc.
- ✅ **Cloudflare is Optional**: CDN cache purging is only needed if you're using a CDN
- ⚠️ **Not Yet Aspire-Friendly**: The backend deployment workflow doesn't yet integrate with .NET Aspire orchestration

---

### Job 3: Build Windows Desktop App

**Platform**: Windows 2025  

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
- **x86 Build**: 32-bit build runs on both 32-bit and 64-bit Windows

---

### Job 4: Build Android App

**Platform**: Ubuntu 24.04  

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

**Step-by-Step Process**:

1. **Setup Apple Development Environment**
   ```yaml
   - Setup .NET SDK
   - Setup Xcode 26.0 (latest)
   - Setup Node.js 24
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
    - uses: actions/download-artifact@v6  # Get pre-built artifact
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
  APP_VERSION = 1.0.0
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

---

### Expected app size

Depending on `dotnet new bit-bp` and `dotnet publish` commands parameters, the app size is expected to be something between the following range:

- **Web** => 3.5MB to 7MB
Enabling/Disabling LLVM during `dotnet publish` command and `--offlineDb` parameter during `dotnet new bit-bp` command have huge impacts.
---
- **Android** => 18MB to 35MB
Enabling/Disabling LLVM during `dotnet publish` command has the most impact. `dotnet new` parameters doesn't have much affect on this. 
---
- **Windows** => 30MB to 55MB
Enabling/Disabling AOT during `dotnet publish` command has the most impact. `dotnet new` parameters or x86/x64 don't have much affect on this.
---
- **iOS/macOS** => 120MB to 130MB
---