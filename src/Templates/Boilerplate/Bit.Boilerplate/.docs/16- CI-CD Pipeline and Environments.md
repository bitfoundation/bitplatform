# Stage 16: CI/CD Pipeline and Environments

Welcome to Stage 16! In this stage, you'll learn how the project handles Continuous Integration/Continuous Deployment (CI/CD) and environment management across different platforms.

---

## Overview

This project includes a comprehensive CI/CD setup that supports:
- **Continuous Integration (CI)**: Automated build and test on every pull request
- **Continuous Deployment (CD)**: Automated deployment to Test and Production environments
- **Multi-Platform Support**: Build pipelines for Web, Windows, Android, iOS, and macOS
- **Environment Configuration**: Consistent environment management across all platforms

---

## 1. Environment Management

### Understanding AppEnvironment

The project uses a custom `AppEnvironment` class to provide consistent environment management across all platforms (server, web, and native apps).

**Location**: [`/src/Shared/Services/AppEnvironment.cs`](/src/Shared/Services/AppEnvironment.cs)

#### Why AppEnvironment?

Unlike ASP.NET Core which uses environment variables, Android, iOS, Windows, and macOS don't support the same concept. `AppEnvironment` provides a unified approach that works everywhere.

#### Supported Environments

The project supports four environments:
1. **Development** - For local development
2. **Test** - For testing/staging environment
3. **Staging** - For pre-production validation (optional)
4. **Production** - For live production environment

#### How It Works

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

#### Environment Configuration in MSBuild

The environment is configured in [`/src/Directory.Build.props`](/src/Directory.Build.props):

```xml
<!-- See Boilerplate.Shared.AppEnvironment for more info. -->
<Environment Condition="'$(Environment)' == '' AND '$(Configuration)' == 'Release'">Production</Environment>
<Environment Condition="'$(Environment)' == '' AND  $(Configuration.Contains('Debug'))">Development</Environment>
```

**Key Point**: The environment is determined at **build time** using MSBuild properties, making it available in both:
- **C# code**: Via `AppEnvironment.Current` or `#if Development` directives
- **MSBuild**: Via `$(Environment)` property

#### Using AppEnvironment in Code

Throughout the codebase, you'll see environment checks like:

```csharp
// Example from AuthManager.cs
Secure = AppEnvironment.IsDevelopment() is false

// Example from ClientExceptionHandlerBase.cs
var isDevEnv = AppEnvironment.IsDevelopment();

// Example from RetryDelegatingHandler.cs
if (request.HasNoRetryPolicyAttribute() || AppEnvironment.IsDevelopment())
    return await base.SendAsync(request, cancellationToken);
```

#### Building for Different Environments

```powershell
# Development (default for Debug configuration)
dotnet build -c Debug

# Production (default for Release configuration)
dotnet publish -c Release

# Test environment
dotnet publish -c Release -p:Environment=Test

# Staging environment
dotnet publish -c Release -p:Environment=Staging
```

---

## 2. Continuous Integration (CI)

### GitHub Actions CI Workflow

**Location**: [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

This workflow runs on:
- Every **pull request**
- **Manual trigger** (workflow_dispatch)

#### What It Does

1. **Checkout Code**: Gets the latest source code
2. **Setup .NET**: Installs .NET SDK using `global.json`
3. **Setup Node.js**: Installs Node.js 23 for JavaScript/TypeScript builds
4. **Workload Restore**: Installs required .NET workloads
5. **Build**: Builds the entire solution (`Boilerplate.slnx`)
6. **Install Playwright**: Installs browser automation tools for testing
7. **Test**: Runs all unit and integration tests
8. **Upload Test Results**: If tests fail, uploads test artifacts for debugging

```yaml
name: Boilerplate CI

on:
  workflow_dispatch:
  pull_request:

jobs:
  build_blazor_server:
    runs-on: ubuntu-24.04
    steps:
      - name: Checkout source code
      - name: Setup .NET
      - name: Setup Node.js
      - name: Workload restore
      - name: Build
      - name: Install Playwright
      - name: Test
      - name: Upload Tests Artifact (on failure)
```

### Azure DevOps CI Pipeline

**Location**: [`.azure-devops/workflows/ci.yml`](.azure-devops/workflows/ci.yml)

Similar to GitHub Actions but configured for Azure DevOps:
- Triggers on: **develop** branch
- Uses Azure DevOps tasks instead of GitHub Actions
- Same build and test steps

---

## 3. Continuous Deployment (CD)

The project includes separate CD workflows for different environments.

### GitHub Actions CD Workflows

#### Production Deployment

**Location**: [`.github/workflows/cd-production.yml`](.github/workflows/cd-production.yml)

```yaml
name: Boilerplate Production CD

on:
  workflow_dispatch:
  push:
    branches: [ "main" ]

jobs:
  build_and_deploy_prod:
    uses: ./.github/workflows/cd-template.yml
    with:
      ENV_NAME: Production
    secrets: inherit
```

- **Triggers**: Push to `main` branch or manual trigger
- **Uses**: Reusable workflow template with `Production` environment

#### Test Deployment

**Location**: [`.github/workflows/cd-test.yml`](.github/workflows/cd-test.yml)

```yaml
name: Boilerplate Test CD

on:
  workflow_dispatch:
  push:
    branches: [ "test" ]

jobs:
  build_and_deploy_test:
    uses: ./.github/workflows/cd-template.yml
    with:
      ENV_NAME: Test
    secrets: inherit
```

- **Triggers**: Push to `test` branch or manual trigger
- **Uses**: Reusable workflow template with `Test` environment

### CD Template Workflow

**Location**: [`.github/workflows/cd-template.yml`](.github/workflows/cd-template.yml)

This is a **reusable workflow** that handles the actual build and deployment. It's called by both production and test workflows.

#### Jobs Overview

The template includes multiple jobs for different platforms:

1. **build_api_blazor** - Build backend API + Blazor Web
2. **deploy_api_blazor** - Deploy to Azure Web App
3. **build_blazor_hybrid_windows** - Build Windows application
4. **build_blazor_hybrid_android** - Build Android application
5. **build_blazor_hybrid_iOS** - Build iOS and macOS applications

---

## 4. Platform-Specific Build Details

### Backend (API + Blazor Web)

#### Build Phase

```yaml
- name: Update core appsettings.json
  uses: devops-actions/variable-substitution@v1.2 
  with:
    files: 'src/**/appsettings*json'
  env:
    WebAppRender.BlazorMode: 'BlazorWebAssembly'
    ServerAddress: ${{ vars.SERVER_ADDRESS }}

- name: Publish
  run: dotnet publish src/Server/Boilerplate.Server.Web/Boilerplate.Server.Web.csproj 
       -c Release --self-contained -r linux-x64 
       -p:Version="${{ vars.APP_VERSION }}" 
       -p:Environment=${{ inputs.ENV_NAME }}

- name: Upload server artifact
  uses: actions/upload-artifact@v5
```

**Key Points**:
- Updates `appsettings.json` with environment-specific values
- Publishes as **self-contained** for Linux
- Passes environment name via `-p:Environment=` parameter
- Uploads artifact for deployment phase

#### Deploy Phase

```yaml
- name: Retrieve server bundle
  uses: actions/download-artifact@v5

- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ vars.APP_SERVICE_NAME }}
    publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
```

**Two-Phase Deployment Benefits**:
1. **Separation of Concerns**: Build and deploy are separate jobs
2. **Security**: Deployment runner doesn't need SDKs installed
3. **Flexibility**: Can use different runners (e.g., lightweight runner for deployment)
4. **Reusability**: Build artifact can be deployed to multiple environments

### Windows Application

```yaml
- name: Publish
  run: |
    cd src\Client\Boilerplate.Client.Windows\
    dotnet publish -c Release -o .\publish-result -r win-x86 
                   -p:Version="${{ vars.APP_VERSION }}" --self-contained 
                   -p:Environment=${{ inputs.ENV_NAME }}
    dotnet tool restore
    dotnet vpk pack -u ${{ vars.APP_ID }} -v "${{ vars.APP_VERSION }}" 
                    -p .\publish-result -e Boilerplate.Client.Windows.exe 
                    -r win-x86 --framework webview2
```

**Key Features**:
- Uses **Velopack** (`vpk`) for installation and auto-updates
- Creates installer package with WebView2 runtime
- Self-contained deployment

### Android Application

```yaml
- name: Extract Android signing key
  uses: timheuer/base64-to-file@v1.2
  with:
    fileDir: './src/Client/Boilerplate.Client.Maui/'
    fileName: 'Boilerplate.keystore'
    encodedString: ${{ secrets.ANDROID_RELEASE_KEYSTORE_FILE_BASE64 }}

- name: Publish aab
  run: dotnet publish src/Client/Boilerplate.Client.Maui/Boilerplate.Client.Maui.csproj 
       -c Release -p:ApplicationId=${{ vars.APP_ID }} 
       -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true 
       -p:AndroidSigningKeyStore="Boilerplate.keystore" 
       -p:Version="${{ vars.APP_VERSION }}" 
       -p:Environment=${{ inputs.ENV_NAME }} 
       -f net10.0-android
```

**Key Features**:
- Publishes as **AAB** (Android App Bundle) for Google Play
- Uses code signing with keystore
- Environment passed to build

### iOS/macOS Application

```yaml
- name: Import Code-Signing Certificates
  uses: apple-actions/import-codesign-certs@v5
  with:
    p12-file-base64: ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64 }}
    p12-password: ${{ secrets.APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD }}

- name: Download Apple Provisioning Profiles
  uses: Apple-Actions/download-provisioning-profiles@v4

- name: Build ipa
  run: dotnet publish src/Client/Boilerplate.Client.Maui/Boilerplate.Client.Maui.csproj 
       -p:RuntimeIdentifier=ios-arm64 -c Release -p:ArchiveOnBuild=true 
       -p:CodesignKey="iPhone Distribution" 
       -p:Version="${{ vars.APP_VERSION }}" 
       -p:Environment=${{ inputs.ENV_NAME }} 
       -f net10.0-ios
```

**Key Features**:
- Imports Apple code signing certificates
- Downloads provisioning profiles
- Creates IPA for App Store distribution

---

## 5. Configuration Variables and Secrets

### GitHub Actions Variables (per environment)

Variables are configured in GitHub repository settings under **Environments**:

- `APP_VERSION` - Application version (e.g., "1.0.0")
- `APP_ID` - Application identifier (e.g., "com.company.app")
- `APP_TITLE` - Application display name
- `SERVER_ADDRESS` - Backend API URL
- `APP_SERVICE_NAME` - Azure App Service name
- `WINDOWS_UPDATE_FILES_URL` - URL for Windows update files
- `IOS_CODE_SIGN_PROVISION` - iOS provisioning profile name

### GitHub Actions Secrets

Secrets are stored securely and used during build/deployment:

- `AZURE_PUBLISH_PROFILE` - Azure Web App publish profile
- `ANDROID_RELEASE_KEYSTORE_FILE_BASE64` - Android signing keystore (base64)
- `ANDROID_RELEASE_KEYSTORE_PASSWORD` - Keystore password
- `ANDROID_RELEASE_SIGNING_PASSWORD` - Key password
- `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_BASE64` - Apple certificate (base64)
- `APPSTORE_CODE_SIGNING_CERTIFICATE_FILE_PASSWORD` - Certificate password
- `APPSTORE_API_KEY_ISSUER_ID` - App Store Connect API issuer
- `APPSTORE_API_KEY_ID` - App Store Connect API key ID
- `APPSTORE_API_KEY_PRIVATE_KEY` - App Store Connect API private key

---

## 6. Azure DevOps CD Pipeline

**Location**: [`.azure-devops/workflows/cd.yml`](.azure-devops/workflows/cd.yml)

Similar structure to GitHub Actions but using Azure DevOps tasks:

```yaml
variables:
  APP_SERVICE_NAME: 'app-service-bp-test'
  AZURE_SUBSCRIPTION: 'bp-test-service-connection'
  ServerAddress: 'https://use-your-api-server-url-here.com/'
  WebAppRender.BlazorMode: 'BlazorWebAssembly'

jobs:
  - job: build_api_blazor
  - job: deploy_api_blazor
  - job: build_blazor_hybrid_windows
  - job: build_blazor_hybrid_android
```

---

## 7. appsettings.json Transformation

During deployment, the workflows use variable substitution to update `appsettings.json` files with environment-specific values:

```yaml
- name: Update core appsettings.json
  uses: devops-actions/variable-substitution@v1.2 
  with:
    files: 'src/**/appsettings*json'
  env:
    ServerAddress: ${{ vars.SERVER_ADDRESS }}
    WebAppRender.BlazorMode: 'BlazorWebAssembly'
    WindowsUpdate.FilesUrl: ${{ vars.WINDOWS_UPDATE_FILES_URL }}
```

This replaces values in all `appsettings*.json` files based on JSON path.

---

## 8. Localization Support

The workflows include automatic translation using the **bit-resx** tool:

```yaml
- name: Use Bit.ResxTranslator
  run: |
    dotnet tool install --global Bit.ResxTranslator
    bit-resx-translate
```

This ensures all resource files are properly translated before building.

---

## 9. Important Notes & Best Practices

### ⚠️ Backend CI/CD Not Yet Aspire-Friendly

**Important**: The current CI/CD workflows are primarily configured for **client platforms** (Android, iOS, Windows, macOS). The backend deployment to Azure is included but may need additional configuration for .NET Aspire projects.

- Backend CD uses Azure App Service deployment
- Azure usage is **completely optional**
- You can deploy to any hosting provider (AWS, GCP, on-premises, etc.)

### ✅ Two-Phase Deployment Pattern

The backend follows a best practice of **two-phase deployment**:

1. **Build Phase** (Job 1):
   - Runs on a runner with .NET SDK, Node.js, etc.
   - Builds and publishes the application
   - Uploads artifacts to GitHub/Azure DevOps

2. **Deploy Phase** (Job 2):
   - Runs on a separate (potentially lightweight) runner
   - Downloads pre-built artifacts
   - Deploys to target environment

**Why This Is Better**:
- **Security**: Deployment runner has minimal software installed and direct access to production
- **Performance**: Deployment runner doesn't need SDKs (faster, lighter)
- **Flexibility**: Can deploy the same artifact to multiple environments
- **Reliability**: Build failures don't affect deployment infrastructure

### 🔄 Environment-Aware Compilation

The environment is baked into the compiled application:

```csharp
// This is determined at BUILD TIME, not runtime
#if Development
    // Development code
#elif Production
    // Production code
#endif
```

This means:
- ✅ You can have different code paths per environment
- ✅ Dead code is eliminated (better performance)
- ✅ Environment detection is instant (no runtime checks)
- ⚠️ You must rebuild for different environments (can't change by config file alone)

---

## 10. Example: Setting Up a New Environment

To add a new environment (e.g., "Staging"):

### Step 1: Update AppEnvironment.cs

Already supports Staging:
```csharp
#elif Staging              // dotnet publish -c Release -p:Environment=Staging
    Staging;
```

### Step 2: Create GitHub Workflow

Create `.github/workflows/cd-staging.yml`:
```yaml
name: Boilerplate Staging CD

on:
  workflow_dispatch:
  push:
    branches: [ "staging" ]

jobs:
  build_and_deploy_staging:
    uses: ./.github/workflows/cd-template.yml
    with:
      ENV_NAME: Staging
    secrets: inherit
```

### Step 3: Configure Environment in GitHub

1. Go to repository **Settings** → **Environments**
2. Create new environment: "Staging"
3. Add variables: `SERVER_ADDRESS`, `APP_VERSION`, etc.
4. Add secrets if needed

### Step 4: Build for Staging

```powershell
dotnet publish -c Release -p:Environment=Staging
```

---

## 11. Hands-On Exploration

To better understand the CI/CD setup:

1. **Review the workflow files**:
   - [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
   - [`.github/workflows/cd-production.yml`](.github/workflows/cd-production.yml)
   - [`.github/workflows/cd-template.yml`](.github/workflows/cd-template.yml)

2. **Check GitHub Actions**:
   - Go to your repository's **Actions** tab
   - See workflow runs, logs, and artifacts

3. **Examine environment configuration**:
   - [`/src/Directory.Build.props`](/src/Directory.Build.props) - MSBuild properties
   - [`/src/Shared/Services/AppEnvironment.cs`](/src/Shared/Services/AppEnvironment.cs) - Environment detection

4. **Try building for different environments**:
   ```powershell
   # Development
   dotnet build -c Debug
   
   # Production
   dotnet publish -c Release
   
   # Test
   dotnet publish -c Release -p:Environment=Test
   ```

5. **Check environment at runtime**:
   - Run the app in different configurations
   - Check the About page to see the current environment
   - Look for environment-specific behavior (e.g., detailed errors in Development)

---

## 12. Common CI/CD Scenarios

### Scenario 1: Hotfix to Production

1. Create hotfix branch from `main`
2. Make changes and test locally
3. Create PR to `main`
4. CI runs automatically on PR
5. Merge PR to `main`
6. CD automatically deploys to Production

### Scenario 2: Feature Testing

1. Create feature branch
2. Make changes
3. Create PR to `test` branch
4. CI validates build and tests
5. Merge to `test` branch
6. CD automatically deploys to Test environment
7. After testing, create PR from `test` to `main`

### Scenario 3: Manual Deployment

1. Go to GitHub Actions
2. Select the desired CD workflow
3. Click "Run workflow"
4. Select branch
5. Click "Run workflow" button

---

## Summary

In this stage, you learned:

✅ **Environment Management**: How `AppEnvironment` provides consistent environment detection across all platforms  
✅ **CI Pipeline**: Automated build and test on every pull request  
✅ **CD Pipelines**: Separate workflows for Test and Production environments  
✅ **Multi-Platform Builds**: Build automation for Web, Windows, Android, and iOS  
✅ **Two-Phase Deployment**: Secure and efficient build-then-deploy pattern  
✅ **Configuration Management**: Using variables and secrets for environment-specific settings  
✅ **Build-Time Environment**: How environment is compiled into the application  

The CI/CD setup provides a solid foundation that you can customize based on your team's needs and infrastructure.

---