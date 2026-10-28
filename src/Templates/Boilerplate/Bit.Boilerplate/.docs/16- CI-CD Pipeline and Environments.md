# Stage 16: CI/CD Pipeline and Environments

Welcome to Stage 16! In this stage, you'll learn about the comprehensive CI/CD pipeline setup and environment configuration system that powers this project. This system provides environment-aware builds and deployments across all platforms.

---

## 📋 Environment Configuration System

### Understanding AppEnvironment

The project uses a unified environment configuration system that works consistently across all platforms - from ASP.NET Core backend to native mobile apps.

**Location**: [`/src/Shared/Infrastructure/Services/AppEnvironment.cs`](/src/Shared/Infrastructure/Services/AppEnvironment.cs)

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

<!--#if (pipeline == "Azure")-->
Your project was generated with `--pipeline Azure`, so it ships **`.azure-devops/workflows/`**, not
`.github/workflows/`:

- **`ci.yml`** — triggered on `develop`. Sets up .NET, lets `bit setup` install what the agent is missing (Node.js,
  the workloads, Playwright's browsers and the HTTPS development certificate), builds `Boilerplate.slnx`, and runs
  the test suite; uploads `src/Tests/TestResults` as a pipeline artifact when the
  tests are what failed.
- **`cd.yml`** — triggered on `main`. Five jobs: `test`, which runs the test suite as `ci.yml` does,
  `build_api_blazor` → `deploy_api_blazor` (Azure App Service), which deploys only once `test` has passed, plus
  `build_blazor_hybrid_windows` and `build_blazor_hybrid_android`.

Configure it in Azure DevOps rather than GitHub:

1. **Pipelines → New pipeline** → point it at `.azure-devops/workflows/ci.yml` (and again for `cd.yml`).
2. **Project settings → Service connections** → create an Azure Resource Manager connection and put its name in
   `AZURE_SUBSCRIPTION` at the top of `cd.yml`, along with `APP_SERVICE_NAME`, `ServerAddress` and
   `WindowsUpdate.FilesUrl`.
3. **Pipelines → Edit → Variables** (or a variable group) → define `APP_VERSION`, `APP_ID` and `APP_TITLE` there,
   **not** in `cd.yml`. A variable declared in the YAML `variables:` block takes priority over the UI and cannot
   be set at queue time, which would freeze the version at whatever the template shipped.
4. **Pipelines → Library → Secure files** → upload `Boilerplate.keystore` for the Android job, and add
   `ANDROID_RELEASE_KEYSTORE_PASSWORD` / `ANDROID_RELEASE_SIGNING_PASSWORD` as secret pipeline variables.
   `bit keys android --alias Boilerplate` creates the keystore and shows its password, which both variables take.
<!--#if (api == "Integrated" && database != "Sqlite" && database != "Other")-->
5. **Pipelines → Edit → Variables** → add `DATABASE_CONNECTION_STRING` as a secret variable: a connection string to
   the production database with the rights to change its schema. `deploy_api_blazor` applies the database migrations
   with it before it deploys.
<!--#endif-->

Two differences from the GitHub pipeline are deliberate and worth knowing: the Azure pipeline has **no iOS/macOS
job** (it would require a macOS agent pool), and it uses a single `main`-triggered CD rather than the
Production/Test pair described below. The rest of this document describes the GitHub Actions pipeline; the build
steps are the same, only the YAML dialect and the place you store secrets differ.

<!--#endif-->
<!--#if (pipeline == "GitHub")-->
The project includes a complete CI/CD pipeline setup using GitHub Actions with **4 workflow files**:
<!--#endif-->

### 1. Continuous Integration (CI) - `ci.yml`

**File**: [`/.github/workflows/ci.yml`](/.github/workflows/ci.yml)

**Triggers**:
- Pull requests to any branch
- Manual workflow dispatch
- `cd-production.yml`, which runs it before every production deployment

**What It Does**:
```
✓ Checks out code
✓ Sets up .NET SDK (from global.json)
✓ Runs bit setup, which installs what the runner is missing: Node.js, the workloads,
  Playwright's browsers with their system libraries, and the HTTPS development certificate
✓ Builds entire solution (Boilerplate.slnx)
✓ Runs all tests (unit + integration + UI tests)
✓ Uploads test results as artifacts if tests fail
```

**Key Configuration**:
- **Runner**: Ubuntu 24.04
- **SDK Version**: Automatically detected from `global.json`
- **Machine setup**: the bit CLI, installed once per job with `dotnet tool install Bit.Cli --version 11.0.0-pre-15`, then `bit setup`,
  which installs only what the runner is missing
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
ci:
  calls: ci.yml
build_and_deploy_prod:
  needs: ci
  calls: cd-template.yml
  with:
    ENV_NAME: "Production"
  secrets: inherit
```

It runs `ci.yml` first, and only when its build and tests pass does it run the reusable deployment template with the Production environment configuration. A failing test never reaches production.

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

This triggers the same reusable template but with Test environment configuration. Unlike `cd-production.yml` it
does not wait for `ci.yml`; copy its `ci` job and `needs: ci` here if Test deployments should wait for the tests too.

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
- **Build Provenance**: In a public repository, each artifact gets a signed attestation of the commit and the run
  that built it, which `gh attestation verify <downloaded artifact .zip> --repo <owner>/<repo>` checks. Private
  repositories need GitHub Enterprise Cloud for attestations, so the step is skipped there. In a public repository
  the About page also shows the commit and the run that built the app, with a link to the attestations, and the
  Windows app shows who signed it once Windows has checked the signature

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
   - bit setup --no-build: Node.js when the runner lacks it and the wasm-tools workload; the publish
     that follows generates the CSS/JS from TypeScript and SCSS in its own build
   ```

2. **Localization with `bit translate`**
   ```bash
   bit translate
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
   # Publish self-contained Linux binary (Using Linux is optional)
   dotnet publish -c Release --self-contained -r linux-x64 \
     -p:Version="1.0.0" -p:Environment=Production
   ```

5. **Artifact Upload**
<!--#if (api == "Integrated" && database != "Sqlite" && database != "Other")-->

6. **Migrations Bundle**
   ```bash
   dnx dotnet-ef@11.0.0-rc.2.26504.105 -- migrations bundle --self-contained --target-runtime linux-x64 --output efbundle
   ```
   - Packs the EF Core migrations into one executable, uploaded as the `migrations-bundle` artifact for Job 2
<!--#endif-->

---

### Job 2: Deploy API + Blazor WebAssembly

**Depends On**: Job `Build API + Blazor WebAssembly` job

**Platform**: Ubuntu 24.04

**Step-by-Step Process**:

1. **Download Build Artifact**
   ```yaml
   - Download server-bundle from previous job
   - Contains complete self-contained application
<!--#if (api == "Integrated")-->
   - Write the environment's APP_CERTIFICATE secret (and APP_CERTIFICATE_RETIRED) next to it as the AppCertificate files
<!--#if (notification == true)-->
   - Put the environment's PRIVATE_VAPIDKEY and VAPID_SUBJECT secrets in its appsettings.json
<!--#endif-->
<!--#endif-->
   ```
<!--#if (api == "Integrated")-->
   - The deploy job applies these secrets, not the build job: anyone who can read the repository can download its build artifacts
<!--#endif-->

<!--#if (api == "Integrated" && database != "Sqlite" && database != "Other")-->
2. **Apply Database Migrations**
   ```yaml
   - Download the migrations bundle the build job made
   - Run it against the database in the DATABASE_CONNECTION_STRING secret
   - Stop here if a migration fails, so the new version never runs on an old database schema
   ```

3. **Azure Web App Deployment** (optional - you can use any hosting)
<!--#else-->
2. **Azure Web App Deployment** (optional - you can use any hosting)
<!--#endif-->
   ```yaml
   - Deploy to Azure Web App
   - Uses publish profile from secrets
   - Deploys to production slot
   - Returns webapp URL
   ```

<!--#if (api == "Integrated" && database != "Sqlite" && database != "Other")-->
4. **CDN Cache Purge**
<!--#else-->
3. **CDN Cache Purge**
<!--#endif-->
   ```yaml
   - Purge Cloudflare cache (if configured)
   - Ensures users get the latest version immediately
   - No stale cached responses
   ```

**Important Notes**:
- ✅ **Azure is Optional**: You can deploy to AWS, Google Cloud, your own servers, Docker, Kubernetes, Windows/IIS etc.
- ✅ **Cloudflare is Optional**: CDN cache purging is only needed if you're using a CDN
- ⚠️ **Not Yet Aspire-Friendly**: The backend deployment workflow doesn't yet integrate with .NET Aspire orchestration
<!--#if (api == "Standalone" && database != "Sqlite" && database != "Other")-->
- ✅ **Database Migrations**: the api deploys in its own `deploy_api` job, which runs the migrations bundle `build_api` made against the database in the `DATABASE_CONNECTION_STRING` secret right before it deploys the api
<!--#endif-->
<!--#if (api == "Standalone")-->
- ✅ **Secrets**: the api's own `deploy_api` job writes the environment's `APP_CERTIFICATE` (and `APP_CERTIFICATE_RETIRED`) next to the api<!--#if (notification == true)-->, and its `PRIVATE_VAPIDKEY` and `VAPID_SUBJECT` into its appsettings.json<!--#endif-->, before the api or its migrations start
<!--#endif-->
<!--#if (database == "Sqlite")-->
- ✅ **Database Migrations**: the app applies them itself when it starts
<!--#endif-->
<!--#if (database == "Other")-->
- ⚠️ **The Database Is Not Migrated Here**: see "Applying Migrations to a Deployed Database" in `.docs/01- Entity Framework Core.md`
<!--#endif-->

---

### Job 3: Build Windows Desktop App

**Platform**: Windows 2025  

**Step-by-Step Process**:

1. **Environment Setup & Configuration**
   ```yaml
   - Setup .NET SDK, then bit setup --no-build for Node.js when the runner lacks it
   - Translate resource files (`bit translate`)
   - Update appsettings.json:
     - ServerAddress: Environment-specific API URL
     - WindowsUpdate.FilesUrl: Auto-update endpoint
   ```

2. **Build & Package with Velopack**
   ```bash
   # Publish for Windows x86 (32-bit for wider compatibility)
   dotnet publish -c Release -r win-x86 --self-contained \
     -p:Version="1.0.0" -p:Environment=Production
   
   # Create installer with Velopack
   dnx vpk@1.3.39-g8d2844f -- pack \
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

**Optional code signing**: Once the `CODE_SIGNING_ACCOUNT` variable is set, Velopack signs the app's executables and
DLLs, `Update.exe` and `Setup.exe` with Azure Artifact Signing, so Windows shows your organization as the publisher and
SmartScreen warns less, and the About page shows who signed the app. Until then nothing is signed. To set it up:

1. Create an Artifact Signing account in Azure, complete its identity validation, and create a Public Trust certificate
   profile.
2. Let the pipeline sign:
   - **GitHub**: create an app registration with a federated credential for each environment that deploys, such as
     `repo:<owner>/<repo>:environment:Production`, give it the **Artifact Signing Certificate Profile Signer** role on the
     certificate profile, and set the `CODE_SIGNING_CLIENT_ID`, `CODE_SIGNING_TENANT_ID` and
     `CODE_SIGNING_SUBSCRIPTION_ID` secrets. No password is stored: the job logs in with GitHub's OIDC token.
   - **Azure DevOps**: give that role to the identity behind the `AZURE_SUBSCRIPTION` service connection.
3. Set the `CODE_SIGNING_ENDPOINT` (the account's URI, such as `https://weu.codesigning.azure.net`),
   `CODE_SIGNING_ACCOUNT` and `CODE_SIGNING_CERTIFICATE_PROFILE` variables.

Each signed file counts against the account's monthly quota. To sign only the executables, add
`--signExclude '\.dll$'` to the `vpk pack` command.

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
   - `bit pipeline` creates the keystore and its three secrets, and `bit keys android` creates one as a file

2. **Build Android App Bundle (AAB)**
```bash
# Install the bit CLI once, then let it install Node.js when missing, the MAUI Android workload
# and what the Android SDK lacks; the publish below generates the CSS/JS files in its own build
dotnet tool install --global Bit.Cli --version 11.0.0-pre-15
bit setup --platforms android --no-build --yes
   
# Publish signed AAB (Or APK if needed)
dotnet publish -c Release \
  -p:ApplicationId=com.company.app \
  -p:AndroidPackageFormat=aab \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="Boilerplate.keystore" \
  -p:AndroidSigningKeyAlias=Boilerplate \
  -p:AndroidSigningKeyPass="${{ secrets.ANDROID_RELEASE_KEYSTORE_PASSWORD }}" \
  -p:AndroidSigningStorePass="${{ secrets.ANDROID_RELEASE_SIGNING_PASSWORD }}" \
  -p:Version="1.0.0" \
  -p:Environment=Production \
  -f net11.0-android
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

**Platform**: macOS 27 (the `xcode-27` runner image)  

**Step-by-Step Process**:

1. **Setup Apple Development Environment**
   ```yaml
   - Setup .NET SDK
   - Setup Xcode 27.0
   - bit setup --no-build: Node.js when the runner lacks it and the MAUI workload
   - Translate resources (`bit translate`)
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
   - No Mac needed to make them: `bit pipeline` creates the distribution certificate, its `.p12` and the App Store
     provisioning profile through the App Store Connect API and stores them, and `bit keys apple` writes them as files

3. **Build iOS App Package (IPA)**
   ```bash
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
     -f net11.0-ios
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
- Uploads artifacts to GitHub (or Azure DevOps)
- **No production access** - isolated from production systems

> **The build phase does not run tests.** `cd-template.yml` goes straight from `bit setup` to
> `dotnet publish`. The tests run in `ci.yml`, on every pull request and, through `cd-production.yml`, before every
> production deployment. `cd-test.yml` does not wait for them, so to gate the Test environment as well, give it the
> same `ci` job or protect the `test` branch with a required status check on `ci.yml`.

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
  runs-on: ubuntu-26.04
  steps:
    - uses: actions/checkout@v7
    - uses: actions/setup-dotnet@v6
    - run: dotnet tool install --global Bit.Cli --version 11.0.0-pre-15
    - run: bit setup --no-build --yes
    - run: dotnet publish ...
    - uses: actions/upload-artifact@v7  # Save artifact

# Phase 2: Deploy (has production access)
deploy_api_blazor:
  needs: build_api_blazor  # Depends on Phase 1
  runs-on: ubuntu-26.04
  steps:
    - uses: actions/download-artifact@v8  # Get pre-built artifact
    - uses: azure/webapps-deploy@v3      # Deploy to production
```

**Result**: Even if an attacker compromises the build agent (which has many tools installed), they cannot access production. Conversely, if the deployment agent is compromised, the attacker cannot inject malicious code into the build process.

---

## 🔐 Required Secrets and Variables

The CI/CD pipeline requires various secrets and variables to be configured in GitHub. These are stored per environment (Production, Test, Staging, etc.).

### How to Configure in GitHub

Run `bit pipeline` in the repository. It reads the secrets the workflows use, the reusable workflows they call included,
creates the environments they deploy to, and stores each secret in its environment. It creates what it can on its own:
the VAPID key pair, the app certificate and the Android signing key, which it also adds to `assetlinks.json`, and with
an App Store Connect API key you give it, the iOS app's signing certificate and provisioning profile. It asks for the
rest, and an empty answer skips one. `bit pipeline --check` only lists what's missing.

Or by hand:

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
  APP_ID = com.myapp                       (bundle id / Velopack package id)
  APP_SERVICE_NAME = my-app-service        (Azure App Service name)
  WINDOWS_UPDATE_FILES_URL = https://api.myapp.com/windows
  OPENAI_ENDPOINT = <optional, only if you use bit translate>

Secrets:
  AZURE_PUBLISH_PROFILE = <production publish profile>
  APP_CERTIFICATE = <the production AppCertificate, its .crt and .key in one PEM>
  APP_CERTIFICATE_RETIRED = <optional - the certificate it replaced, see AppCertificate.md>
  PUBLIC_VAPIDKEY = <production VAPID public key>
  PRIVATE_VAPIDKEY = <production VAPID private key>
  VAPID_SUBJECT = <mailto: address or https URL the push services can reach you at>
  OPENAI_APIKEY = <optional - when unset, the bit translate step translates nothing>
```

The mobile/desktop jobs additionally need `ANDROID_RELEASE_KEYSTORE_FILE_BASE64`,
`ANDROID_RELEASE_KEYSTORE_PASSWORD`, `ANDROID_RELEASE_SIGNING_PASSWORD` and, for iOS, the
`APPSTORE_*` code-signing secrets plus the `IOS_CODE_SIGN_PROVISION` variable.
<!--#if (api == "Standalone")-->
Because this project uses `--api Standalone`, the api is deployed separately and also needs
`API_APP_SERVICE_NAME` (variable) and `API_AZURE_PUBLISH_PROFILE` (secret).
<!--#endif-->
<!--#if (database != "Sqlite" && database != "Other")-->
Every environment that deploys also needs the `DATABASE_CONNECTION_STRING` secret: a connection string to that
environment's database with the rights to change its schema. The deploy job applies the database migrations with it
before it deploys the new version, and stops the deployment when it is missing or wrong.

The migrations bundle runs on the pipeline's runner, so the database has to accept connections from it; if it
doesn't, use a self-hosted runner, or delete the two migration steps and migrate another way. The bundle starts the
app's own startup code, so it runs inside the published folder, where it finds the app's settings and its
`AppCertificate` files; if your certificate reaches the server some other way, put it next to the bundle as well.
And since it runs while the previous version is still serving, every migration has to keep that version working:
add a column in one release and drop the old one in a later release, rather than renaming it in one go.
<!--#endif-->

**Test Environment**:
```
Variables:
  SERVER_ADDRESS = https://test-api.myapp.com
  APP_VERSION = 1.0.0
  APP_TITLE = My App (Test)

Secrets:
  AZURE_PUBLISH_PROFILE = <test publish profile>
  APP_CERTIFICATE = <the test environment's own AppCertificate>
  PUBLIC_VAPIDKEY = <test VAPID public key>
  PRIVATE_VAPIDKEY = <test VAPID private key>
  VAPID_SUBJECT = <mailto: address or https URL>
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

Depending on `bit new` and `dotnet publish` commands parameters, the app size is expected to be something between the following range:

- **Web** => 3.5MB to 7MB
Enabling/Disabling LLVM during `dotnet publish` command and `--offlineDb` parameter during `bit new` command have huge impacts.
---
- **Android** => 18MB to 35MB
Enabling/Disabling LLVM during `dotnet publish` command has the most impact. `bit new` parameters doesn't have much affect on this. 
---
- **Windows** => 30MB to 55MB
Enabling/Disabling AOT during `dotnet publish` command has the most impact. `bit new` parameters or x86/x64 don't have much affect on this.
---
- **iOS/macOS** => 120MB to 130MB
---

### AI Wiki

Ask your own question [here](https://bitplatform.dev/ask)

---
