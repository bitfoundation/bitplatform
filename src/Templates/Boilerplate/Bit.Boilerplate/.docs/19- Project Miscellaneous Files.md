# Stage 19: Project Miscellaneous Files

Welcome to **Stage 19** of the getting started guide! In this stage, we'll explore the various configuration and miscellaneous files at the root of the project. Understanding these files is essential for maintaining code quality, managing dependencies, and configuring your development environment.

---

## 1. Configuration Files

### 1.1 `.editorconfig`

**Location**: [`/.editorconfig`](/.editorconfig)

**Purpose**: The `.editorconfig` file ensures **consistent coding style** across different editors and IDEs. This is crucial for team collaboration and maintaining code quality.

**Key Features**:

- **Indentation**: Uses 4 spaces for all files
- **C# Conventions**: Enforces modern C# coding standards
  - `var` preferences
  - Expression-bodied members
  - Pattern matching
  - Null-checking preferences
  - File-scoped namespaces (enforced as warning)
- **Formatting Rules**: 
  - New line preferences (braces, else, catch, finally)
  - Indentation for switch cases
  - Space preferences around operators
- **Naming Conventions**: PascalCase for constant fields

**Example from the file**:

```properties
[*.cs]
# File-scoped namespaces are enforced
csharp_style_namespace_declarations = file_scoped:warning

# var preferences
csharp_style_var_for_built_in_types = true:silent
csharp_style_var_when_type_is_apparent = true:silent

# New line before open braces
csharp_new_line_before_open_brace = all
```

**Why it matters**: When you write code, your IDE will automatically apply these rules, ensuring consistency without manual effort. This prevents "code style wars" in pull requests.

---

### 1.2 `global.json`

**Location**: [`/global.json`](/global.json)

**Purpose**: Specifies the **.NET SDK version** required for the project.

**Content**:

```json
{
    "sdk": {
        "version": "10.0.100",
        "rollForward": "latestFeature"
    }
}
```

**Key Properties**:

- **`version`**: The specific .NET SDK version (currently .NET 10 RC)
- **`rollForward`**: Set to `"latestFeature"` - allows using newer feature releases automatically

**Why it matters**: This ensures all team members and CI/CD pipelines use the same SDK version, preventing "works on my machine" issues.

```json
{
    "sdk": {
        "version": "10.0.100",
        "rollForward": "latestFeature"
    },
    "test": {
        "runner": "Microsoft.Testing.Platform"
    }
}
```

**Additional Properties**:

- **`test.runner`**: Configures the .NET test runner to use **Microsoft.Testing.Platform** (MSTest v4), which provides better performance and features compared to older test runners.

---

## 2. Solution Files

### 2.1 `Boilerplate.sln`

**Location**: [`/Boilerplate.sln`](/Boilerplate.sln)

**Purpose**: The **full solution file** for Visual Studio that includes ALL projects in the workspace.

**Contains**:
- Server projects (`Boilerplate.Server.Web`, `Boilerplate.Server.Api`, `Boilerplate.Server.Shared`, `Boilerplate.Server.AppHost`)
- Client projects (`Boilerplate.Client.Core`, `Boilerplate.Client.Web`, `Boilerplate.Client.Maui`, `Boilerplate.Client.Windows`)
- Shared project (`Boilerplate.Shared`)
- Tests project (`Boilerplate.Tests`)
- Solution folders organizing configuration files

**When to use**: Open this when you need to work across all platforms and projects simultaneously.

---

### 2.2 `Boilerplate.Web.slnf`

**Location**: [`/Boilerplate.Web.slnf`](/Boilerplate.Web.slnf)

**Purpose**: A **solution filter** that includes only the projects needed for **web development**.

**Why it exists**: Loading all projects (including MAUI, Windows) can be slow and resource-intensive. This filter speeds up your development experience when you're only working on web features.

**Projects included**:
- `Boilerplate.Server.Web`
- `Boilerplate.Server.Api`
- `Boilerplate.Server.Shared`
- `Boilerplate.Server.AppHost`
- `Boilerplate.Client.Core`
- `Boilerplate.Client.Web`
- `Boilerplate.Shared`
- `Boilerplate.Tests`

**When to use**: This is the **default solution** in VS Code (see `.vscode/settings.json`). Use it for faster builds when you're not working on mobile/desktop features.

---

### 2.3 `Boilerplate.slnx`

**Location**: [`/Boilerplate.slnx`](/Boilerplate.slnx)

**Purpose**: The new **XML-based solution format** introduced in Visual Studio 2025. It's more human-readable and git-friendly than the traditional `.sln` format.

**Benefit**: Easier to merge in source control, more maintainable, and supports modern Visual Studio features.

---

## 3. Build Configuration Files

### 3.1 `Directory.Build.props`

**Location**: [`/src/Directory.Build.props`](/src/Directory.Build.props)

**Purpose**: A **shared MSBuild properties file** that automatically applies to ALL projects in the `src/` directory and its subdirectories. This prevents repetitive configuration in individual `.csproj` files.

**Key Configurations**:

#### Common Properties
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

#### Versioning
```xml
<Version>1.0.0</Version>
<ApplicationDisplayVersion>$(Version)</ApplicationDisplayVersion>
```

**Important**: Change the version here to update it across all projects.

#### Environment Configuration
```xml
<Environment Condition="'$(Environment)' == '' AND '$(Configuration)' == 'Release'">Production</Environment>
<Environment Condition="'$(Environment)' == '' AND $(Configuration.Contains('Debug'))">Development</Environment>
```

This creates compile-time constants like `DEBUG`, `Development`, `Production` that you can use in C#:

```csharp
#if DEBUG
    // Debug-only code
#endif

#if Production
    // Production-only code
#endif
```

#### Platform Detection
```xml
<DefineConstants Condition="$(TargetFramework.Contains('-android'))">$(DefineConstants);Android</DefineConstants>
<DefineConstants Condition="$(TargetFramework.Contains('-ios'))">$(DefineConstants);iOS</DefineConstants>
<DefineConstants Condition="$(TargetFramework.Contains('-windows'))">$(DefineConstants);Windows</DefineConstants>
```

This allows platform-specific code:

```csharp
#if Android
    // Android-specific code
#endif
```

#### Global Using Directives
```xml
<Using Include="System.Net.Http" />
<Using Include="System.Text.Json" />
<Using Include="Boilerplate.Shared.Dtos" />
<Using Include="Boilerplate.Shared.Exceptions" />
<!-- ... and many more -->
```

These namespaces are **automatically imported** in every C# file, eliminating the need for repetitive `using` statements.

---

### 3.2 `Directory.Packages.props`

**Location**: [`/src/Directory.Packages.props`](/src/Directory.Packages.props)

**Purpose**: Enables **Central Package Management (CPM)** for NuGet packages. All package versions are defined in one place.

**How it works**:

Instead of this in each `.csproj`:
```xml
<PackageReference Include="Bit.BlazorUI" Version="10.3.0" />
```

You write this in `.csproj`:
```xml
<PackageReference Include="Bit.BlazorUI" />
```

And the version is defined centrally in `Directory.Packages.props`:
```xml
<PackageVersion Include="Bit.BlazorUI" Version="10.3.0" />
```

**Benefits**:
- **Single source of truth** for package versions
- **Easier updates**: Update version once, affects all projects
- **Prevents version conflicts** between projects

**Example packages included**:

```xml
<PackageVersion Include="Bit.BlazorUI" Version="10.3.0" />
<PackageVersion Include="Bit.Butil" Version="10.3.0" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageVersion Include="Hangfire.AspNetCore" Version="1.8.21" />
<PackageVersion Include="Riok.Mapperly" Version="4.3.0" />
<!-- ... and many more -->
```

**To update a package**: Simply change the version in this file and rebuild.

---

## 4. Cleanup Scripts

### 4.1 `Clean.bat` (Windows)

**Location**: [`/Clean.bat`](/Clean.bat)

**Purpose**: A PowerShell-based cleanup script for **Windows** that removes build artifacts and temporary files.

**What it does**:
1. Deletes untracked CSS, JS, and map files (generated files not in Git)
2. Runs `dotnet clean` on all `.csproj` files
3. Removes common build folders: `bin`, `obj`, `node_modules`, `.vs`, etc.
4. Deletes empty directories

**When to use**: 
- When your project won't build due to corrupted cache
- Before switching branches with major structural changes
- When you want to ensure a completely clean build

**⚠️ Important**: Close all IDEs (Visual Studio, VS Code) before running this script to prevent file locks.

---

### 4.2 `Clean.sh` (macOS/Linux)

**Location**: [`/Clean.sh`](/Clean.sh)

**Purpose**: The **macOS/Linux equivalent** of `Clean.bat`.

**Usage**:
```bash
chmod +x Clean.sh  # Make it executable (first time only)
./Clean.sh         # Run the script
```

---

## 5. Localization Configuration

### 5.1 `Bit.ResxTranslator.json`

**Location**: [`/Bit.ResxTranslator.json`](/Bit.ResxTranslator.json)

**Purpose**: Configuration for the **bit-resx** CLI tool that uses AI to automatically translate `.resx` resource files.

**Configuration**:

```json
{
    "DefaultLanguage": "en",
    "SupportedLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de" ],
    "ResxPaths": [ "/src/**/*.resx" ],
    "OpenAI": {
        "Model": "gpt-4.1-mini",
        "Endpoint": "https://models.inference.ai.azure.com",
        "ApiKey": null
    }
}
```

**How it works**:

1. You write strings in English in `AppStrings.resx`
2. Run the `bit-resx` tool
3. It automatically creates translated versions:
   - `AppStrings.nl.resx` (Dutch)
   - `AppStrings.fa.resx` (Persian)
   - `AppStrings.sv.resx` (Swedish)
   - `AppStrings.hi.resx` (Hindi)
   - `AppStrings.zh.resx` (Chinese)
   - And more...

**Supported Languages**: Currently configured for 9 languages (Dutch, Persian, Swedish, Hindi, Chinese, Spanish, French, Arabic, German)

**AI Provider**: Uses OpenAI's GPT-4.1-mini model (or Azure OpenAI)

**Learn more**: See [bit-resx documentation](https://github.com/bitfoundation/bitplatform/tree/develop/src/ResxTranslator)

**Installation and Usage**:

```bash
# Install the bit-resx CLI tool globally
dotnet tool install --global Bit.ResxTranslator

# Run the translator (from the project root directory)
bit-resx
```

**To add a new language**:
1. Add its ISO code to `SupportedLanguages` (e.g., `"it"` for Italian)
2. Update `CultureInfoManager.SupportedCultures` in the `Shared` project
3. Update `.Client.Maui/Platforms/Android/MainActivity.cs` if needed
4. Run `bit-resx` to generate the translation

---

## 6. IDE Configuration Files

### 6.1 `.vsconfig`

**Location**: [`/.vsconfig`](/.vsconfig)

**Purpose**: Specifies the **required Visual Studio workloads and extensions** for this project.

**Content**:

```json
{
    "components": [
        "Microsoft.VisualStudio.Workload.NetWeb",
        "Microsoft.VisualStudio.Workload.NetCrossPlat",
        "Component.Android.SDK.MAUI"
    ],
    "extensions": [
        "https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager"
    ]
}
```

**How it helps**: When someone clones the repository and opens it in Visual Studio, they'll be prompted to install the required workloads and extensions automatically.

**Required Workloads**:
- **NetWeb**: ASP.NET and web development
- **NetCrossPlat**: .NET MAUI development for cross-platform apps
- **Android SDK**: Android development support

**Required Extensions**:
- **ResX Manager**: A Visual Studio extension for managing resource files (`.resx`) with a user-friendly interface

---

### 6.2 `settings.VisualStudio.json`

**Location**: [`/settings.VisualStudio.json`](/settings.VisualStudio.json)

**Purpose**: **Project-specific settings** for Visual Studio (not VS Code).

**Key Settings**:

```json
{
    "languages.defaults.tabs.tabSize": 4,
    "languages.defaults.general.lineNumbers": true,
    "environment.documents.saveWithSpecificEncoding": true,
    "environment.documents.saveEncoding": "utf-8-nobom;65001",
    "debugging.hotReload.enableHotReload": true,
    "debugging.hotReload.applyOnFileSave": true,
    "debugging.general.disableJITOptimization": true,
    "debugging.hotReload.enableForNoDebugLaunch": true,
    "projectsAndSolutions.aspNetCore.general.hotReloadCssChanges": true,
    "copilot.general.completions.enableNextEditSuggestions": true,
    "copilot.general.editor.enableAdaptivePaste": true
}
```

**Notable Features**:
- **Hot Reload**: Enabled for faster development (apply changes without restarting)
- **Hot Reload CSS**: CSS changes apply immediately
- **UTF-8 Encoding**: All files saved with UTF-8 without BOM
- **GitHub Copilot**: Enhanced features enabled (Next Edit Suggestions, Adaptive Paste)

---

### 6.3 `.vscode/` Folder

**Location**: [`/.vscode/`](/.vscode/)

**Purpose**: Contains **VS Code workspace settings** and configurations.

#### 6.3.1 `.vscode/mcp.json`

**Location**: [`/.vscode/mcp.json`](/.vscode/mcp.json)

**Purpose**: Configuration for **Model Context Protocol (MCP) servers** that extend GitHub Copilot's capabilities with specialized knowledge and tools.

**Content**:

```json
{
    "servers": {
        "DeepWiki": {
            "type": "sse",
            "url": "https://mcp.deepwiki.com/mcp"
        }
    }
}
```

**What is MCP?**:
- **Model Context Protocol**: A standardized way to connect AI assistants (like GitHub Copilot) to external data sources and tools
- **Server-Sent Events (SSE)**: The communication protocol used to stream data from the MCP server

**DeepWiki Server**:
- **Purpose**: Provides GitHub Copilot with deep knowledge about specific repositories
- **In this project**: Configured to access the `bitfoundation/bitplatform` and `riok/mapperly` repositories
- **How it helps**: When you ask Copilot questions about Bit.BlazorUI components, Mapperly, or other bitplatform features, it can query the DeepWiki server for accurate, up-to-date documentation

**Example Usage**:
- Ask: *"How do I use BitDataGrid with server-side paging?"*
- Copilot uses the DeepWiki MCP server to fetch relevant information from the bitplatform repository
- You get accurate answers based on the actual source code and documentation

**Adding More MCP Servers**:
You can add additional MCP servers to extend Copilot's capabilities further. For example:
- Database documentation servers
- Internal company knowledge bases
- API documentation servers

**Learn More**: Visit [modelcontextprotocol.io](https://modelcontextprotocol.io) for more information about MCP.

---


#### 6.3.2 `.vscode/settings.json`

```json
{
    "liveSassCompile.settings.watchOnLaunch": true,
    "dotnet.defaultSolution": "Boilerplate.Web.slnf",
    "dotnet.unitTests.runSettingsPath": "src/Tests/.runsettings",
    "chat.tools.autoApprove": true,
    "github.copilot.chat.codesearch.enabled": true,
    "csharp.preview.improvedLaunchExperience": true,
    "explorer.fileNesting.enabled": true,
    "explorer.fileNesting.patterns": {
        "*.resx": "$(capture).*.resx",
        "*.razor": "$(capture).*.razor, $(capture).razor.cs, $(capture).razor.scss",
        "*.scss": "$(capture).*.scss, $(capture).css, $(capture).css.map",
        "*.json": "$(capture).*.json"
    }
}
```

**Key Features**:

- **Live SASS Compilation**: Automatically compiles SCSS files on launch
- **Default Solution**: Uses `Boilerplate.Web.slnf` for faster load times
- **File Nesting**: Groups related files together in the explorer
  - `Component.razor`, `Component.razor.cs`, `Component.razor.scss` nest under `Component.razor`
  - `AppStrings.resx`, `AppStrings.fa.resx`, `AppStrings.nl.resx` nest under `AppStrings.resx`
- **GitHub Copilot**: Auto-approve tool usage, code search enabled

#### 6.3.3 `.vscode/extensions.json`

**Recommended Extensions**:

```json
{
    "recommendations": [
        "GitHub.copilot",
        "glenn2223.live-sass",
        "GitHub.copilot-chat",
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-dotnettools.dotnet-maui",
        "ms-azuretools.vscode-docker",
        "ms-vscode-remote.remote-containers",
        "ms-dotnettools.blazorwasm-companion",
        "ms-dotnettools.vscode-dotnet-runtime"
    ]
}
```

When you open the project in VS Code, you'll be prompted to install these extensions.

#### 6.3.4 `.vscode/tasks.json`

**Pre-configured Tasks**:

- **`before-build`**: Runs TypeScript compilation and SCSS processing (runs automatically on folder open)
- **`build`**: Builds the `Boilerplate.Server.Web` project
- **`generate-resx-files`**: Generates C# code from `.resx` files
- **`run`**: Starts the application
- **`run-tests`**: Runs all tests

**How to use**: Press `Ctrl+Shift+P` → `Tasks: Run Task` → Select a task

#### 6.3.5 `.vscode/launch.json`

**Debug Configurations**:

```json
{
    "configurations": [
        {
            "name": "C#: Boilerplate.Server.Web Debug",
            "type": "dotnet",
            "request": "launch",
            "projectPath": "${workspaceFolder}/src/Server/Boilerplate.Server.Web/Boilerplate.Server.Web.csproj"
        },
        {
            "name": "C#: Boilerplate.Server.Api Debug",
            "type": "dotnet",
            "request": "launch",
            "projectPath": "${workspaceFolder}/src/Server/Boilerplate.Server.Api/Boilerplate.Server.Api.csproj"
        },
        {
            "name": ".NET MAUI",
            "type": "maui",
            "request": "launch",
            "preLaunchTask": "maui: Build"
        }
    ]
}
```

**How to use**: Press `F5` or go to Run and Debug panel → Select a configuration → Start debugging

---

## 7. Source Control

### 7.1 `.gitignore`

**Location**: [`/.gitignore`](/.gitignore)

**Purpose**: Specifies which files and folders Git should **ignore** (not track in version control).

**What's ignored**:

- **Build artifacts**: `bin/`, `obj/`, `*.cache`
- **IDE files**: `.vs/`, `.idea/`
- **Dependencies**: `node_modules/`, `packages/`
- **Generated files**: `*.css` (from SCSS), `*.js` (from TypeScript), `*.map`
- **User-specific files**: `*.user`, `*.suo`
- **Database files**: `*.mdf`, `*.ldf`
- **Environment files**: `.env`
- **Custom Boilerplate patterns**:
  - `*Resource.designer.cs` (auto-generated from `.resx`)
  - `/src/Client/Boilerplate.Client.Core/Scripts/*.js` (generated from TypeScript)
  - `/src/Client/Boilerplate.Client.Maui/Platforms/Android/google-services.json` (sensitive Firebase config)

**Why it matters**: Keeps your repository clean and prevents accidentally committing sensitive data or large binary files.

---

## 8. Documentation

### 8.1 `README.md`

**Location**: [`/README.md`](/README.md)

**Purpose**: The project's **welcome file** displayed on GitHub/Azure DevOps.

**Contains**:
- Welcome message
- Template creation command (showing exactly how this project was generated)
- Link to comprehensive documentation at [bitplatform.dev/templates](https://bitplatform.dev/templates/overview)
- Information about the template version used

**Example**:

````markdown
This project gets generated by bit-bp template v-10.3.0 using the following command
```bash
dotnet new bit-bp
    --name Boilerplate
    --database SqlServer
    --filesStorage S3
    --module Admin
    --captcha reCaptcha
    --sample
    --sentry
    --appInsights
    --signalR
    --offlineDb
    --ads
```
````

This is useful for remembering how the project was initially configured.

---

## 9. Spell Checking

### 9.1 `vs-spell.dic`

**Location**: [`/vs-spell.dic`](/vs-spell.dic)

**Purpose**: A **custom dictionary** for spell checkers (Visual Studio, VS Code extensions) containing technical terms and project-specific words.

**Examples of words included**:

```plaintext
webp
resx
nameof
Mapperly
Blazor
Json
editorconfig
Hangfire
rendermode
sqlite
Besql
appsettings
Butil
webauthn
scss
webassembly
odata
totp
aspnetcore
sqlserver
postgres
slnx
slnf
```

**Why it matters**: Prevents false-positive spell check warnings for technical terms like `webauthn`, `odata`, `Blazor`, etc.

**Referenced in**: `.editorconfig` with `spelling_exclusion_path = vs-spell.dic`

---

## 10. CI/CD Pipeline

### 10.1 `.github/workflows/`

**Location**: [`/.github/workflows/`](/.github/workflows/)

**Purpose**: Contains **GitHub Actions workflows** for Continuous Integration and Continuous Deployment.

**Workflow Files**:

1. **`ci.yml`**: Continuous Integration
   - Runs on every push/pull request
   - Builds all projects
   - Runs tests
   - Ensures code quality

2. **`cd-test.yml`**: Deploy to Test environment
   - Triggered manually or on merge to `develop` branch
   - Deploys to test/staging servers

3. **`cd-production.yml`**: Deploy to Production
   - Triggered manually or on merge to `main` branch
   - Deploys to production servers

4. **`cd-template.yml`**: Template for creating custom CD workflows
   - Copy and customize for your specific deployment needs

**Note**: The current workflows are configured for client platforms (Android, iOS, Windows, macOS) but are not yet fully Aspire-friendly for backend deployment. Backend CI/CD may need additional configuration.

**Best Practice**: The workflows use a **2-phase deployment**:
1. **Build Phase**: Build the project and upload artifacts to GitHub/Azure DevOps
2. **Deployment Phase**: Download artifacts and deploy to target environment

**Why 2 phases?**: 
- Use different runners for build and deployment
- Deployment runner can be lightweight (no SDKs needed)
- More secure - deployment agent doesn't need full build tools

---

## 11. Dev Containers

### 11.1 `.devcontainer/`

**Location**: [`/.devcontainer/`](/.devcontainer/)

**Purpose**: Configuration for **VS Code Dev Containers** and **GitHub Codespaces**.

**What it does**: Allows you to develop inside a Docker container with all dependencies pre-installed, ensuring **consistent development environments** across all team members.

**Benefits**:
- No need to install .NET SDK, Node.js, or other tools locally
- Works identically on Windows, macOS, and Linux
- One-click setup in GitHub Codespaces
- Isolated from your host system

**How to use**:
1. Install Docker Desktop and the "Dev Containers" extension in VS Code
2. Open the project in VS Code
3. Click "Reopen in Container" when prompted
4. Wait for the container to build
5. Start developing!

---
