# Stage 16: CI/CD Pipeline and Environments

Welcome to Stage 16! In this stage, you'll learn about the comprehensive CI/CD pipeline setup and environment configuration system that powers this project. This system provides environment-aware builds and deployments across all platforms.

---

## 📋 Environment Configuration System

### Understanding AppEnvironment

The project uses a unified environment configuration system that works consistently across all platforms - from ASP.NET Core backend to native mobile apps.

**Location**: [`/src/Shared/Services/AppEnvironment.cs`](/src/Shared/Services/AppEnvironment.cs)

**Key Features**:
- **Cross-Platform Consistency**: Unlike ASP.NET Core which uses environment variables, Android, iOS, Windows, and macOS don't support the same concept. `AppEnvironment` provides a unified abstraction.
- **Build-Time Configuration**: Environment is determined at build/publish time using MSBuild properties.
- **Compile-Time Constants**: Uses C# preprocessor directives for zero runtime overhead.

**Available Environments**:
```csharp
- Development  // dotnet publish -c Debug
- Test         // dotnet publish -c Release -p:Environment=Test
- Staging      // dotnet publish -c Release -p:Environment=Staging
- Production   // dotnet publish -c Release (default)
```

**Usage Examples**:
```csharp
// Check current environment
if (AppEnvironment.IsDevelopment())
{
    // Development-specific code
}

// Get environment name
string env = AppEnvironment.Current; // "Development", "Production", etc.

// Set environment (done automatically at startup)
AppEnvironment.Set(builder.Environment.EnvironmentName);
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

**Key Benefits**:
1. **Available in Both C# and MSBuild**: Environment information is accessible in:
   - C# code (via `AppEnvironment.Current`)
   - MSBuild scripts (via `$(Environment)` property)
   - Razor components (via `@AppEnvironment.Current`)

2. **Environment-Specific Code**: Write conditional code that compiles differently per environment:
```csharp
#if Development
    // This code only exists in Development builds
    services.AddDeveloperTools();
#endif
```

3. **Platform Detection**: Similar constants for platform-specific code:
```csharp
#if Android
    // Android-specific code
#elif iOS
    // iOS-specific code
#elif Windows
    // Windows-specific code
#endif
```

---

## 🔄 CI/CD Workflow Overview

The project includes a complete CI/CD pipeline setup using GitHub Actions with four workflow files:

### 1. Continuous Integration (CI)

**File**: [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

**Triggers**:
- Pull requests
- Manual workflow dispatch

**What it does**:
```yaml
✓ Checks out code
✓ Sets up .NET SDK and Node.js
✓ Restores workloads
✓ Builds entire solution (Boilerplate.slnx)
✓ Installs Playwright for UI testing
✓ Runs all tests (unit + integration + UI tests)
✓ Uploads test results if tests fail
```

**Key Features**:
- Runs on Ubuntu 24.04
- Uses .NET SDK version from `global.json`
- Uses Node.js 23
- Automated test execution with artifact upload on failure

### 2. Production Deployment

**File**: [`.github/workflows/cd-production.yml`](.github/workflows/cd-production.yml)

**Triggers**:
- Push to `main` branch
- Manual workflow dispatch

**What it does**:
```yaml
✓ Triggers the reusable CD template
✓ Sets ENV_NAME to "Production"
✓ Deploys to production environment
```

### 3. Test Environment Deployment

**File**: [`.github/workflows/cd-test.yml`](.github/workflows/cd-test.yml)

**Triggers**:
- Push to `test` branch
- Manual workflow dispatch

**What it does**:
```yaml
✓ Triggers the reusable CD template
✓ Sets ENV_NAME to "Test"
✓ Deploys to test environment
```

### 4. Reusable Deployment Template

**File**: [`.github/workflows/cd-template.yml`](.github/workflows/cd-template.yml)

This is the core deployment workflow that handles all platforms. It's a **reusable workflow** called by the production and test workflows.

---

## 🏗️ Build and Deployment Pipeline

### Job 1: Build API + Blazor Web

**Platform**: Ubuntu 24.04

**Steps**:

1. **Setup Environment**
   ```yaml
   - Checkout code
   - Setup .NET SDK
   - Setup Node.js 23
   ```

2. **Localization**
   ```yaml
   - Install Bit.ResxTranslator tool
   - Translate resource files automatically
   ```

3. **Configuration Substitution**
   ```yaml
   - Update appsettings.json files with environment variables
   - Configure server address, VAPID keys, Blazor mode
   ```

4. **Build Process**
   ```yaml
   - Install WASM tools
   - Generate CSS/JS files (BeforeBuildTasks)
   - Publish with: -c Release --self-contained -r linux-x64
   - Environment: -p:Environment=${{ inputs.ENV_NAME }}
   ```

5. **Artifact Upload**
   ```yaml
   - Upload server bundle with hidden files
   - Includes .well-known folder for WebAuthn/ACME
   ```

**Key Configuration**:
```yaml
- BlazorMode: Set to 'BlazorWebAssembly'
- ServerAddress: Environment-specific URL
- Version: From GitHub variables
- Self-contained: Yes (includes .NET runtime)
- Runtime: linux-x64
```

### Job 2: Deploy API + Blazor Web

**Depends On**: Build API + Blazor Web

**Steps**:

1. **Download Artifact**
   ```yaml
   - Retrieve server bundle from previous job
   ```

2. **Azure Deployment**
   ```yaml
   - Deploy to Azure Web App
   - Uses publish profile from secrets
   - Deploys to production slot
   ```

3. **CDN Cache Purge**
   ```yaml
   - Purge Cloudflare cache
   - Ensures users get latest version immediately
   ```

**Important Note**: This demonstrates Azure deployment, but it's **completely optional**. You can deploy to:
- AWS
- Google Cloud
- Your own servers
- Docker containers
- Any hosting provider

### Job 3: Build Windows Desktop App

**Platform**: Windows 2025

**Steps**:

1. **Setup & Configuration**
   ```yaml
   - Setup .NET and Node.js
   - Translate resources
   - Update appsettings.json with WindowsUpdate.FilesUrl
   ```

2. **Build & Package**
   ```yaml
   - Generate CSS/JS files
   - Publish: -r win-x86 --self-contained
   - Create installer with Velopack (dotnet vpk pack)
   - Include app icon and WebView2 framework
   ```

3. **Upload Artifact**
   ```yaml
   - Upload Windows installer to artifacts
   ```

**Velopack Features**:
- Creates auto-updating Windows installer
- Packages WebView2 runtime
- Supports delta updates
- Version management

### Job 4: Build Android App

**Platform**: Ubuntu 24.04

**Steps**:

1. **Setup Signing**
   ```yaml
   - Extract Android signing key from base64 secret
   - Configure keystore for release signing
   ```

2. **Build AAB (Android App Bundle)**
   ```yaml
   - Install maui-android workload
   - Install Android SDK platform tools
   - Generate CSS/JS files
   - Publish with signing configuration
   ```

**Signing Configuration**:
```yaml
- AndroidPackageFormat: aab
- AndroidKeyStore: true
- AndroidSigningKeyAlias: Boilerplate
- Passwords from GitHub secrets
```

3. **Upload Artifact**
   ```yaml
   - Upload signed .aab file
   - Ready for Google Play Store
   ```

### Job 5: Build iOS and macOS Apps

**Platform**: macOS 15

**Steps**:

1. **Setup Apple Environment**
   ```yaml
   - Setup .NET SDK
   - Setup Xcode 26.0
   - Setup Node.js 23
   ```

2. **Code Signing**
   ```yaml
   - Import code-signing certificates (P12)
   - Download provisioning profiles from App Store Connect
   - Configure with Apple API credentials
   ```

3. **Build IPA**
   ```yaml
   - Install maui workload
   - Generate CSS/JS files
   - Publish for iOS (ios-arm64)
   - Archive and sign with distribution certificate
   ```

4. **Upload Artifact**
   ```yaml
   - Upload signed .ipa file
   - Ready for App Store submission
   ```

**Apple Requirements**:
- Distribution certificate
- Provisioning profile
- App Store Connect API credentials
- Bundle ID configuration

---

## 🎯 Two-Phase Deployment Architecture

The workflow follows a **best practice two-phase deployment** pattern:

### Phase 1: Build
- Runs on feature-rich build agents
- Has full SDK installations (.NET, Node.js, Android SDK, Xcode)
- Performs compilation, bundling, packaging
- Uploads artifacts to GitHub/Azure DevOps

### Phase 2: Deploy
- Runs on lightweight deployment agents
- No SDKs required - only deployment tools
- **More Secure**: Limited access, fewer attack vectors
- Downloads pre-built artifacts and deploys

**Why This Matters**:
```
Build Agent (Heavy)           Deploy Agent (Light & Secure)
├─ .NET SDK                   ├─ No SDKs needed
├─ Node.js                    ├─ Only deployment tools
├─ Android SDK                ├─ Direct production access
├─ Xcode                      └─ Minimal attack surface
└─ Build tools
```

The deployment agent has **direct access to production**, so keeping it minimal and SDK-free reduces security risks.

---

## 🔐 Required Secrets and Variables

### GitHub Secrets (per environment)

**Backend Deployment**:
- `AZURE_PUBLISH_PROFILE`: Azure Web App publish profile
- `CLOUDFLARE_ZONE`: Cloudflare zone ID (optional)
- `CLOUDFLARE_TOKEN`: Cloudflare API token (optional)

**Android Signing**:
- `ANDROID_RELEASE_KEYSTORE_FILE_BASE64`: Base64-encoded keystore
- `ANDROID_RELEASE_KEYSTORE_PASSWORD`: Keystore password
- `ANDROID_RELEASE_SIGNING_PASSWORD`: Key password

**iOS Signing**:
- `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64`: P12 certificate
- `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD`: Certificate password
- `APPSTORE_API_KEY_ISSUER_ID`: App Store Connect API issuer ID
- `APPSTORE_API_KEY_ID`: App Store Connect API key ID
- `APPSTORE_API_KEY_PRIVATE_KEY`: App Store Connect private key

**App Configuration**:
- `PUBLIC_VAPIDKEY`: Web push VAPID public key

### GitHub Variables (per environment)

- `APP_VERSION`: Application version (e.g., "1.0.0")
- `APP_ID`: Bundle/package identifier (e.g., "com.company.app")
- `APP_TITLE`: Application display name
- `SERVER_ADDRESS`: API server URL
- `WINDOWS_UPDATE_FILES_URL`: Windows auto-update endpoint
- `APP_SERVICE_NAME`: Azure App Service name
- `IOS_CODE_SIGN_PROVISION`: iOS provisioning profile name

---

## 📝 Environment-Specific Configuration

### Build-Time Configuration Substitution

During CI/CD, the workflow modifies appsettings.json files:

```yaml
- name: Update core appsettings.json
  uses: devops-actions/variable-substitution@v1.2 
  with:
    files: 'src/**/appsettings*json'
  env:
    ServerAddress: ${{ vars.SERVER_ADDRESS }}
    WebAppRender.BlazorMode: 'BlazorWebAssembly'
    AdsPushVapid.PublicKey: ${{ secrets.PUBLIC_VAPIDKEY }}
```

This replaces values in the JSON files before building, allowing environment-specific configuration.

---

## 🚀 Platform Support Matrix

| Platform | Build Runner | Artifact Type | Distribution |
|----------|--------------|---------------|--------------|
| **API + Blazor** | Ubuntu 24.04 | Self-contained Linux binary | Azure Web App, Docker, Any Linux host |
| **Windows** | Windows 2025 | Velopack installer (.exe) | Direct download, auto-update |
| **Android** | Ubuntu 24.04 | Android App Bundle (.aab) | Google Play Store |
| **iOS** | macOS 15 | iOS App Package (.ipa) | Apple App Store |
| **macOS** | macOS 15 (same as iOS) | macOS App Package | Apple App Store |

---

## ⚠️ Important Notes

### Backend CI/CD Limitations

**Current State**:
- ✅ Client platforms (Android, iOS, Windows, macOS) have full CI/CD
- ⚠️ Backend deployment is **not yet Aspire-friendly**
- ⚠️ Azure deployment is **completely optional**

**What This Means**:
- The backend CI/CD workflows demonstrate deployment to Azure Web Apps
- If you're using .NET Aspire for local development, you'll need to adapt the deployment
- You can deploy to any hosting provider (AWS, GCP, your own servers, Docker, Kubernetes)
- The current setup is a **starting point**, not a requirement

**Best Practice Applied**:
Even though Aspire integration is pending, the workflow demonstrates the **two-phase deployment** best practice:
1. Build job: Compile and package (uses feature-rich runner)
2. Deploy job: Download artifact and deploy (uses lightweight, secure runner)

This separation means the agent with production access doesn't need SDKs installed, reducing security risks.

### Customizing for Your Needs

**You can**:
- Use different hosting providers
- Add staging environments
- Integrate with your preferred CI/CD platform (Azure DevOps, GitLab CI, Jenkins)
- Modify build configurations
- Add additional build steps

**The workflows are templates** - adapt them to your infrastructure and requirements.

---

## 🎓 Key Takeaways

1. **Unified Environment System**: `AppEnvironment` provides consistent environment detection across all platforms - server, web, mobile, and desktop.

2. **Build-Time Configuration**: Environment is set at build time using `-p:Environment=Production`, creating optimized, environment-specific builds.

3. **Cross-Platform CI/CD**: Single workflow template handles server backend, Blazor WebAssembly, Android, iOS, and Windows builds.

4. **Security Best Practices**: Two-phase deployment keeps production-access agents lightweight and secure.

5. **Flexible Configuration**: Multi-layered appsettings.json system with environment-specific overrides.

6. **Complete Automation**: From code commit to app store submission, the entire pipeline is automated.

7. **Environment Awareness Everywhere**: You can write environment-specific code in C#, use environment variables in MSBuild, and access environment info in Razor components.

---

## 🔗 Real-World Usage

### Example: Adding a New Environment

To add a "Staging" environment:

1. **Update AppEnvironment.cs** (already supported)
2. **Create appsettings.Staging.json** files where needed
3. **Add GitHub environment** called "Staging" with variables/secrets
4. **Create workflow file**:
```yaml
name: Boilerplate Staging CD
on:
  push:
    branches: [ "staging" ]
jobs:
  build_and_deploy_staging:
    uses: ./.github/workflows/cd-template.yml
    with:
      ENV_NAME: Staging
    secrets: inherit
```

5. **Build for staging**:
```bash
dotnet publish -c Release -p:Environment=Staging
```

### Example: Checking Environment in Code

```csharp
// In a controller
public IActionResult GetConfig()
{
    if (AppEnvironment.IsProduction())
    {
        return Ok(new { Mode = "Production", DetailedErrors = false });
    }
    else
    {
        return Ok(new { Mode = AppEnvironment.Current, DetailedErrors = true });
    }
}
```

```xml
<!-- In a Razor component -->
@if (AppEnvironment.IsDevelopment())
{
    <BitMessageBar MessageBarType="BitMessageBarType.Warning">
        Running in Development mode
    </BitMessageBar>
}
```

```csharp
// Platform-specific code
#if Android
    var dataPath = FileSystem.AppDataDirectory;
#elif iOS
    var dataPath = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)[0].Path;
#elif Windows
    var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
```

---