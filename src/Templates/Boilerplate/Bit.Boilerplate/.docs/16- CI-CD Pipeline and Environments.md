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
- Push to `develop` branch (automatic): merging a pull request into `develop` updates the Test environment
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
Since `develop` should take changes only through pull requests whose CI passed, protect it with a required status
check on `ci.yml`.

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
- **Build Provenance**: In a public repository, each client artifact gets a signed attestation of the commit and the
  run that built it, which `gh attestation verify <downloaded artifact .zip> --repo <owner>/<repo>` checks. Private
  repositories need GitHub Enterprise Cloud for attestations, so the step is skipped there. In a public repository
  the About page also shows the commit and the run that built the app, with a link to the attestations, and the
  Windows app shows who signed it once Windows has checked the signature

**Jobs Overview**:
<!--#if (aspire == true)-->
- **build_server** → **deploy_server**: the server, as container images in GitHub Container Registry and a Docker Compose project that the environment's own server runs
<!--#endif-->
- **build_blazor_hybrid_windows**: Windows desktop app (.exe installer)
- **build_blazor_hybrid_android**: Android app bundle (.aab for Google Play)
- **build_blazor_hybrid_iOS**: iOS app package (.ipa for App Store) and macOS app

---

## 🏗️ Detailed Build and Deployment Pipeline

<!--#if (aspire == true)-->
### Job 1: Build the Server

**Platform**: Ubuntu, on a GitHub-hosted runner

**Step-by-Step Process**:

1. **Environment Setup**
   ```yaml
   - Checkout source code
   - Setup .NET SDK (from global.json)
   - bit setup --no-build: Node.js when the runner lacks it and the wasm-tools workload
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
   - VAPID public key: For web push notifications
   ```
   - These values are built into the images, so each environment gets images of its own

4. **Build and Push the Images**
   ```bash
   # The app host builds every project's container image and pushes it to GitHub Container Registry,
   # as ghcr.io/<owner>/<repo>/serverweb:<environment>-<commit>
   aspire do push
   ```
   - The app version (`APP_VERSION`) and the environment reach the build through `Directory.Build.rsp`

5. **Publish the Docker Compose Project**
   ```bash
   aspire publish -o aspire-output
   ```
   - Writes `docker-compose.yaml` and a `.env` that lists every value the deployment needs, uploaded as the
     `compose` artifact
   - The apps run with `ASPNETCORE_ENVIRONMENT` set to the environment (`Test` or `Production`), which the server also
     takes as its `AppEnvironment`

---

### Job 2: Deploy the Server

**Depends On**: Job `Build the Server`

**Platform**: the environment's own server, through a self-hosted runner labeled `Test` or `Production`. The server
needs Docker with its Compose plugin and curl, and nothing else: no SDK and no source code.

**Step-by-Step Process**:

1. **Download the Compose Project**, and log in to GitHub Container Registry with the job's own read-only token
2. **Write the Deploy Folder**, `~/deployments/<project>-<environment>`: the Compose file, a `.env` with every value
   the Compose project lists (from the environment's secrets; a missing one stops the deployment), the app settings
   files and the app certificate
3. **Apply Database Migrations**: a one-shot `migrations` container runs the api's own image with
   `MigrateDatabaseAndExit=true`. When it fails, the deployment stops and the running version keeps serving
4. **Start the New Version**: `docker compose up --wait`, which fails unless every container is healthy, and prints
   the containers' logs when it does
5. **Check the Public Address**: poll `/alive` at `SERVER_ADDRESS` for up to five minutes
<!--#if (cloudflare == true)-->
6. **CDN Cache Purge**: purge the Cloudflare cache, so users get the new version immediately
<!--#endif-->

**Important Notes**:
- ✅ **No Inbound Port**: the runner pulls its jobs from GitHub and the Cloudflare Tunnel's connector dials out, so
  the server accepts no connection from the internet. The `deploy` agent skill (`.github/agents/deploy.agent.md`)
  sets up the server, the runner, the tunnel and the secrets
- ✅ **Database Migrations**: every deployment applies them before the new version starts, while the previous version
  is still serving, so every migration has to keep that version working: add a column in one release and drop the old
  one in a later release, rather than renaming it in one go. The project needs its first migration before the first
  deployment (`bit new` creates `Initial`)
- ⚠️ **A Short Outage**: Compose replaces the app's containers in place, so requests fail for the seconds the new
  ones take to start
- ✅ **Rollback**: deploy an earlier commit again, whose images are still in the registry. Migrations only go forward
- ✅ **Data**: what the server stores lives in Docker volumes named after the Compose project, so back them up off the
  server. Renaming the app host project renames the volumes
<!--#if (database == "Other")-->
- ⚠️ **Your Database Provider**: the app reads its connection string as `otherdb`, from the
  `DATABASE_CONNECTION_STRING` secret, and the migrations container calls `MigrateAsync` on it
<!--#endif-->
<!--#else-->
### The Server

This project has no .NET Aspire app host, so the CD deploys no server: the server ships as a Docker Compose project
that Aspire publishes from the app host. Bring .NET Aspire into the project, and the `deploy` agent skill
(`.github/agents/deploy.agent.md`) sets up the rest.
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

> **The build phase does not run tests.** `cd-template.yml` goes straight from `bit setup` to building the
> images and the apps. The tests run in `ci.yml`, on every pull request and, through `cd-production.yml`, before every
> production deployment. `cd-test.yml` does not wait for them, so to gate the Test environment as well, give it the
> same `ci` job or protect the `develop` branch with a required status check on `ci.yml`.

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

<!--#if (aspire == true)-->
**Example from the Workflow**:
```yaml
# Phase 1: Build (no production access)
build_server:
  runs-on: ubuntu-26.04
  steps:
    - uses: actions/checkout@v7
    - uses: actions/setup-dotnet@v6
    - run: bit setup --no-build --yes
    - run: aspire do push                    # Build the images and push them to the registry
    - run: aspire publish -o aspire-output   # The Docker Compose project
    - uses: actions/upload-artifact@v7

# Phase 2: Deploy (has production access)
deploy_server:
  needs: build_server                     # Depends on Phase 1
  runs-on: [self-hosted, Production]      # The environment's own server
  steps:
    - uses: actions/download-artifact@v8  # Get the Docker Compose project
    - run: docker compose pull && docker compose run --rm migrations && docker compose up -d --wait
```

<!--#endif-->
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
  SERVER_ADDRESS = https://myapp.com
  APP_VERSION = 1.0.0
  APP_TITLE = My App
  APP_ID = com.myapp                       (bundle id / Velopack package id)
  WINDOWS_UPDATE_FILES_URL = https://myapp.com/windows
  OPENAI_ENDPOINT = <optional, only if you use bit translate>

Secrets:
  OPENAI_APIKEY = <optional - when unset, the bit translate step translates nothing>
```

The mobile/desktop jobs additionally need `ANDROID_RELEASE_KEYSTORE_FILE_BASE64`,
`ANDROID_RELEASE_KEYSTORE_PASSWORD`, `ANDROID_RELEASE_SIGNING_PASSWORD` and, for iOS, the
`APPSTORE_*` code-signing secrets plus the `IOS_CODE_SIGN_PROVISION` variable.
<!--#if (aspire == true)-->

The server's deployment reads these too, set per environment:

| Name | Kind | Holds |
| --- | --- | --- |
| `SMTP_CONNECTION_STRING` | secret | The SMTP server the app sends email through, such as `Endpoint=smtp://host:587;UserName=...;Password=...` |
| `APP_CERTIFICATE_CRT`, `APP_CERTIFICATE_KEY` | secret | The PEM text of this environment's own `AppCertificate` pair, never the development one |
| `SERVERWEB_SETTINGS` | secret, optional | Any other setting of the web app, one `Section__Key=value` line each |
<!--#if (api == "Standalone")-->
| `SERVERAPI_SETTINGS` | secret, optional | The same, for the api |
<!--#endif-->
<!--#if (database == "SqlServer")-->
| `DATABASE_PASSWORD` | secret | The database server's password, which SQL Server holds to its complexity rules; it takes effect only while the database volume is still empty |
<!--#elif (database == "PostgreSQL" || database == "MySql")-->
| `DATABASE_PASSWORD` | secret | The database server's password; it takes effect only while the database volume is still empty |
<!--#elif (database == "Other")-->
| `DATABASE_CONNECTION_STRING` | secret | The connection string the app reads as `otherdb` |
<!--#endif-->
<!--#if (filesStorage == "S3")-->
| `S3_CONNECTION_STRING` | secret | The S3-compatible storage, such as Cloudflare R2, in the format of the samples in the server's `appsettings.json` |
<!--#elif (filesStorage == "AzureBlobStorage")-->
| `AZURE_BLOB_STORAGE_CONNECTION_STRING` | secret | The Azure Storage account the files go to |
<!--#endif-->
<!--#if (redis == true)-->
| `REDIS_CACHE_PASSWORD`, `REDIS_PERSISTENT_PASSWORD` | secret | The passwords of the two Redis containers |
<!--#endif-->
<!--#if (cloudflare == true)-->
| `CLOUDFLARE_TUNNEL_TOKEN` | secret | The connector token of the environment's Cloudflare Tunnel |
| `CLOUDFLARE_ZONE`, `CLOUDFLARE_TOKEN` | secret | The zone, and an API token that may purge its cache |
<!--#endif-->
<!--#if (notification == true)-->
| `PUBLIC_VAPIDKEY`, `PRIVATE_VAPIDKEY` | secret | The environment's web push key pair |
| `VAPID_SUBJECT` | variable | The contact the push services see, such as `mailto:you@example.com` |
<!--#endif-->
<!--#endif-->

**Test Environment**:
```
Variables:
  SERVER_ADDRESS = https://test.myapp.com
  APP_VERSION = 1.0.0
  APP_TITLE = My App (Test)
```

Every secret above gets a value of its own for Test: its own database password, certificate, keys and tunnel.

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
