# Stage 19: Project Miscellaneous Files

This document covers various miscellaneous configuration files and utilities in the project that are essential for development workflow, build process, code quality, and project management. Understanding these files will help you customize the project to your needs and maintain consistency across your team.

---

## 1. Solution Files

### `Boilerplate.sln`
The main Visual Studio solution file that includes all projects in the solution.

**When to use:**
- Opening the complete solution in Visual Studio
- Building the entire application with all projects

**Includes:**
- All client projects (Core, Web, MAUI, Windows)
- All server projects (Api, Web, Shared, AppHost)
- Shared libraries and test projects

---

### `Boilerplate.slnx`
A newer solution format introduced in Visual Studio 2022. This format is:
- More human-readable and Git-friendly
- Simpler and easier to merge
- Optimized for modern Visual Studio features

**Key features:**
- Cleaner XML structure
- Better merge conflict resolution
- Faster solution load times

---

### `Boilerplate.Web.slnf`
A **solution filter** that includes only web-related projects for faster builds and improved focus.

**Included projects:**
- `Boilerplate.Client.Core`
- `Boilerplate.Client.Web`
- `Boilerplate.Server.Api`
- `Boilerplate.Server.Web`
- `Boilerplate.Server.Shared`
- `Boilerplate.Shared`
- `Boilerplate.Tests`

**Benefits:**
- Faster build and load times
- Reduced memory usage in IDE
- Better focus on web development
- Ideal for web-only development scenarios

**How to use in VS Code:**
```json
// .vscode/settings.json
"dotnet.defaultSolution": "Boilerplate.Web.slnf"
```

---

## 2. Build Configuration Files

### `Directory.Build.props`
Located in `src/Directory.Build.props`, this file contains shared MSBuild properties for all projects in the solution.

**Key configurations:**

#### General Settings:
```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>14.0</LangVersion>
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

#### Environment Configuration:
- Automatically sets `Environment` based on `Configuration`
- `Debug` → `Development`
- `Release` → `Production`
- Can be overridden with `-p:Environment=Staging`

#### Conditional Compilation Symbols:
- Platform symbols: `Android`, `iOS`, `Windows`, `Mac`
- Environment symbols: `Development`, `Production`
- Configuration symbols: `Debug`, `Release`, `DebugBlazorServer`
- Feature symbols: `InvariantGlobalization`

#### Global Usings:
Automatically includes common namespaces in all C# files:
- `System.Net.Http`
- `System.Text.Json`
- `Microsoft.Extensions.DependencyInjection`
- `Boilerplate.Shared.Dtos`
- `Boilerplate.Shared.Resources`
- And more...

#### Build Optimizations:
- Hot Reload configuration
- Compression settings
- Documentation generation
- Warning and error handling

**Custom Build Tasks:**

1. **DeleteResidualRazorCssFiles**: Cleans up orphaned `.razor.css` files before build to prevent build errors.

**How to override:**
```bash
# Override environment
dotnet build -p:Environment=Staging

# Enable invariant globalization (smaller app size, no localization)
dotnet build -p:InvariantGlobalization=true

# Custom version
dotnet build -p:Version=2.0.0
```

---

### `Directory.Packages.props`
Located in `src/Directory.Packages.props`, this file centralizes NuGet package version management for the entire solution.

**Key benefits:**
- Single source of truth for package versions
- Prevents version conflicts between projects
- Easier dependency updates
- Better dependency management

**Package categories:**

#### Bit Platform Packages:
```xml
<PackageVersion Include="Bit.BlazorUI" Version="10.0.0-pre-08" />
<PackageVersion Include="Bit.Butil" Version="10.0.0-pre-08" />
<PackageVersion Include="Bit.Bswup" Version="10.0.0-pre-08" />
<PackageVersion Include="Bit.Besql" Version="10.0.0-pre-08" />
```

#### Database Providers (Conditional):
```xml
<!-- SQL Server -->
<PackageVersion Condition=" '$(database)' == 'SqlServer' " 
    Include="Microsoft.EntityFrameworkCore.SqlServer" 
    Version="10.0.0-rc.2.25502.107" />

<!-- PostgreSQL -->
<PackageVersion Condition=" '$(database)' == 'PostgreSQL' " 
    Include="Npgsql.EntityFrameworkCore.PostgreSQL" 
    Version="10.0.0-rc.2" />

<!-- MySQL -->
<PackageVersion Condition=" '$(database)' == 'MySql' " 
    Include="Pomelo.EntityFrameworkCore.MySql" 
    Version="9.0.0" />
```

#### Core Framework:
- ASP.NET Core
- Blazor WebAssembly
- Entity Framework Core
- .NET MAUI

#### Third-Party Integrations:
- Hangfire (Background jobs)
- Sentry (Error tracking)
- Application Insights (Monitoring)
- SignalR (Real-time communication)

#### Utilities:
- Mapperly (Object mapping)
- Humanizer (String humanization)
- QRCoder (QR code generation)
- FluentEmail (Email sending)

**How to add a new package:**
1. Add version to `Directory.Packages.props`:
```xml
<PackageVersion Include="MyPackage" Version="1.0.0" />
```

2. Reference in project file (no version needed):
```xml
<PackageReference Include="MyPackage" />
```

---

### `global.json`
Specifies the .NET SDK version to use for the project.

```json
{
    "sdk": {
        "version": "10.0.100-rc.2.25502.107",
        "rollForward": "latestFeature"
    }
}
```

**Purpose:**
- Ensures all developers use the same SDK version
- Prevents build inconsistencies
- Controls SDK feature availability

**Roll-forward policy:**
- `latestFeature`: Uses the latest installed SDK with matching major.minor version
- Other options: `patch`, `minor`, `major`, `latestMinor`, `latestMajor`, `disable`

---

## 3. Code Quality and Style

### `.editorconfig`
Defines coding standards and style rules for the entire codebase.

**Key configurations:**

#### Universal Settings:
```properties
[*]
indent_style = space
indent_size = 4
```

#### C# Code Style:
```properties
[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_for_built_in_types = true:silent
csharp_prefer_braces = true:silent
```

#### .NET Conventions:
```properties
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:silent
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent
```

#### Formatting Rules:
```properties
csharp_new_line_before_open_brace = all
csharp_space_after_keywords_in_control_flow_statements = true
```

#### Custom Diagnostics:
```properties
dotnet_diagnostic.NonAsyncEFCoreMethodsUsageAnalyzer.severity = error
```

**Benefits:**
- Consistent code style across the team
- Automatic formatting in IDE
- Reduced code review discussions about style
- Better Git diffs (consistent formatting)

**Severity levels:**
- `error`: Build fails
- `warning`: Build succeeds with warnings
- `suggestion`: IDE shows hints
- `silent`: Fixes available but not shown

---

### `vs-spell.dic`
Custom dictionary for Visual Studio's spell checker.

**Contains:**
- Technical terms and acronyms
- Framework and library names
- Project-specific terminology
- Common programming abbreviations

**Examples:**
```
webp, resx, nameof, Mapperly, Blazor
Postgre, sqlite, postgres, mssql
appsettings, webassembly, odata
```

**How to add words:**
1. Right-click on a "misspelled" word in Visual Studio
2. Select "Add to Dictionary"
3. Word is automatically added to `vs-spell.dic`

---

## 4. Version Control

### `.gitignore`
Specifies intentionally untracked files that Git should ignore.

**Key patterns:**

#### Build Outputs:
```
bin/
obj/
[Dd]ebug/
[Rr]elease/
```

#### IDE Files:
```
.vs/
*.suo
*.user
.idea/
```

#### Generated Files:
```
*.map
*Resource.designer.cs
/**/*.css
```

#### Project-Specific:
```
/src/Client/Boilerplate.Client.Core/Scripts/*.js
/src/Client/Boilerplate.Client.Core/wwwroot/scripts/app*.js
.env
```

#### Dependencies:
```
node_modules/
**/packages/*
```

**Important notes:**
- CSS files are ignored (generated from SCSS)
- JavaScript files in certain directories are ignored (generated from TypeScript)
- `.env` files are ignored (contain sensitive data)
- Resource designer files are ignored (auto-generated)

---

## 5. IDE Configuration

### Visual Studio Code Configuration

Located in `.vscode/` directory:

#### `settings.json`
```jsonc
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
        "*.json": "$(capture).*.json, $(capture).*.json"
    }
}
```

**Key features:**
- **Live SASS Compilation**: Automatically compiles SCSS to CSS on save
- **File Nesting**: Groups related files (`.razor`, `.razor.cs`, `.razor.scss`) together
- **Default Solution**: Uses solution filter for faster loads
- **Copilot Integration**: Enhanced GitHub Copilot features
- **Markdown Preview**: Opens `.md` files in preview mode by default

#### `extensions.json`
Recommended VS Code extensions:
```jsonc
{
    "recommendations": [
        "GitHub.copilot",
        "GitHub.copilot-chat",
        "glenn2223.live-sass",
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

**When you open the project:**
VS Code will prompt you to install these recommended extensions automatically.

#### `tasks.json`
Defines build and run tasks for the project (covered in Stage 16: CI/CD Pipeline).

#### `launch.json`
Debug configurations for various scenarios (covered in Stage 16: CI/CD Pipeline).

#### `mcp.json`
Model Context Protocol configuration for GitHub Copilot integrations.

---

### Visual Studio Configuration

#### `settings.VisualStudio.json`
Visual Studio specific settings:
```json
{
    "languages.defaults.tabs.tabSize": 4,
    "languages.defaults.general.lineNumbers": true,
    "environment.documents.saveWithSpecificEncoding": true,
    "environment.documents.saveEncoding": "utf-8-nobom;65001",
    "debugging.hotReload.enableHotReload": true,
    "debugging.hotReload.applyOnFileSave": true,
    "copilot.general.completions.enableNextEditSuggestions": true,
    "copilot.general.editor.enableAdaptivePaste": true
}
```

**Features:**
- Hot Reload enabled and applied on save
- UTF-8 encoding without BOM
- Enhanced Copilot features
- JIT optimization disabled for debugging

---

## 6. Localization Configuration

### `Bit.ResxTranslator.json`
Configuration for automated resource file translation using AI.

```json
{
    "DefaultLanguage": "en",
    "SupportedLanguages": [ "nl", "fa", "sv", "hi", "zh", "es", "fr", "ar", "de" ],
    "ResxPaths": [ "/src/**/*.resx" ],
    "ChatOptions": {
        "Temperature": "0"
    },
    "OpenAI": {
        "Model": "gpt-4.1-mini",
        "Endpoint": "https://models.inference.ai.azure.com",
        "ApiKey": null
    }
}
```

**Purpose:**
- Automates translation of `.resx` resource files
- Supports multiple AI providers (OpenAI, Azure OpenAI)
- Maintains consistency across translations

**Supported Languages:**
- `nl`: Dutch (Nederlands)
- `fa`: Persian (Farsi)
- `sv`: Swedish (Svenska)
- `hi`: Hindi
- `zh`: Chinese
- `es`: Spanish (Español)
- `fr`: French (Français)
- `ar`: Arabic
- `de`: German (Deutsch)

**How to add a new language:**
1. Add language code to `SupportedLanguages` array
2. Update `MainActivity.cs` (MAUI) `DataPathPrefixes`
3. Update `CultureInfoManager.cs` `SupportedCultures`
4. Run translation tool

**To run the translator:**
```bash
dotnet run --project path/to/Bit.ResxTranslator.csproj
```

**Important:**
- Set `ApiKey` in environment variable or user secrets
- `Temperature: 0` ensures consistent translations
- Never commit API keys to source control

---

### `dotnet.config`
Configures .NET testing platform:
```plaintext
[dotnet.test.runner]
name = "Microsoft.Testing.Platform"
```

**Purpose:**
- Specifies the test runner for the project
- Uses the modern `Microsoft.Testing.Platform` (MSTest v4+)
- Provides better performance and features than legacy runners

---

## 7. Cleanup Utilities

### `Clean.bat` (Windows)
PowerShell-based cleanup script for Windows.

**What it does:**
1. Deletes untracked generated files (CSS, JS, map files)
2. Runs `dotnet clean` on all `.csproj` files
3. Removes build artifacts (`bin`, `obj`, `node_modules`, `.vs`, etc.)
4. Deletes empty directories

**Usage:**
```bash
Clean.bat
```

**⚠️ Important:**
- Close Visual Studio before running
- Closes IDEs prevent file deletion conflicts
- Cannot be undone (no recycle bin)

---

### `Clean.sh` (macOS/Linux)
Bash-based cleanup script for Unix-like systems.

**What it does:**
1. Runs `dotnet clean` on all `.csproj` files
2. Removes specified directories (`bin`, `obj`, `node_modules`, `.vs`, etc.)
3. Deletes specified file types (`.csproj.user`, `Resources.designer.cs`, CSS, JS, map files)
4. Removes empty directories

**Usage:**
```bash
chmod +x Clean.sh  # Make executable (first time only)
./Clean.sh
```

**⚠️ Important:**
- Close VS for Mac or other IDEs before running
- More aggressive than Windows version (deletes all CSS/JS files, not just untracked)

---

## 8. Documentation Files

### `README.md`
The project's main documentation entry point.

**Contains:**
- Welcome message
- Template generation command with all options used
- Link to comprehensive documentation
- Contact information

**Template generation command example:**
```bash
dotnet new bit-bp \
    --name Boilerplate \
    --database SqlServer \
    --filesStorage S3 \
    --pipeline GitHub \
    --module Admin \
    --aspire true \
    --signalR true \
    --sample true
```

**Documentation link:**
[bitplatform.dev/templates](https://bitplatform.dev/templates/overview)

---

### `.docs/` Directory
Contains comprehensive stage-by-stage documentation (like this document).

**Structure:**
- `00- Interactive Wiki.md`: Introduction and overview
- `01- Entity Framework Core.md`: Database and EF Core
- `02- DTOs, Mappers, and Mapperly.md`: Data transfer patterns
- ... (continues through all stages)
- `25- RAG - Semantic Search with Vector Embeddings (Advanced).md`

**Purpose:**
- Provides detailed explanations of every aspect of the project
- Serves as both learning material and reference
- Helps onboard new team members
- Documents architectural decisions

---

## 9. GitHub Configuration

### `.github/copilot-instructions.md`
Instructions for GitHub Copilot to follow when working with this project.

**Contains:**
- Core principles for AI assistance
- Technology stack overview
- Project structure explanation
- Mandatory workflows
- Coding conventions and best practices
- Specific instructions for common tasks

**Purpose:**
- Ensures consistent AI-generated code
- Teaches Copilot project-specific patterns
- Reduces need for manual corrections
- Improves development velocity

---

### `.github/prompts/`
Contains specialized prompt templates for GitHub Copilot.

**Available prompts:**

1. **`scaffold.prompt.md`**: Generates complete CRUD implementation for a new entity
2. **`resx.prompt.md`**: Moves hardcoded strings to resource files
3. **`bitify.prompt.md`**: Modernizes pages using Bit.BlazorUI components
4. **`getting-started.prompt.md`**: Getting started guide

**Usage:**
```
Run .github/prompts/scaffold.prompt.md for "Product" entity
```

See Stage 18 (Other Available Prompt Templates) for detailed documentation.

---

### `.github/workflows/`
GitHub Actions CI/CD workflow definitions.

**Workflows:**

#### `ci.yml`
Continuous Integration workflow:
- Runs on every push and pull request
- Builds all projects
- Runs unit and integration tests
- Validates code quality

#### `cd-test.yml`
Continuous Deployment to Test environment:
- Deploys to test/staging environment
- Runs after successful CI
- May require manual approval

#### `cd-production.yml`
Continuous Deployment to Production:
- Deploys to production environment
- Requires manual approval
- Includes rollback procedures

#### `cd-template.yml`
Template for creating new deployment workflows.

See Stage 16 (CI/CD Pipeline and Environments) for detailed documentation.

---

## 10. Best Practices and Tips

### Build Optimization

1. **Use Solution Filters**: When working on specific parts of the application:
   ```bash
   dotnet build Boilerplate.Web.slnf
   ```

2. **Skip Heavy Projects**: Exclude MAUI project if not needed:
   ```bash
   dotnet build /p:SkipMauiBuild=true
   ```

3. **Parallel Builds**: Enable parallel project builds:
   ```bash
   dotnet build -m
   ```

### Code Quality

1. **Run EditorConfig**: Ensure `.editorconfig` is respected:
   - Visual Studio: Tools → Options → Text Editor → C# → Code Style
   - VS Code: Install "EditorConfig for VS Code" extension

2. **Enable Code Analysis**: Add to project files:
   ```xml
   <EnableNETAnalyzers>true</EnableNETAnalyzers>
   <AnalysisLevel>latest</AnalysisLevel>
   ```

3. **Use Custom Spell Dictionary**: Add project-specific terms to `vs-spell.dic`

### Version Control

1. **Before Committing**:
   - Review `.gitignore` is working correctly
   - Ensure no sensitive data (API keys, passwords) in commits
   - Check that generated files aren't tracked

2. **Clean Working Directory**:
   ```bash
   git clean -fdx  # Remove all untracked files (careful!)
   ./Clean.bat     # Or use project clean script
   ```

### Dependency Management

1. **Update All Packages**:
   ```bash
   # Update to latest versions
   dotnet outdated
   
   # Update specific package version in Directory.Packages.props
   # Then restore
   dotnet restore
   ```

2. **Check for Vulnerabilities**:
   ```bash
   dotnet list package --vulnerable
   ```

3. **Audit Dependencies**:
   ```bash
   dotnet list package --outdated
   ```

### Localization

1. **Add New Language**:
   - Update `Bit.ResxTranslator.json`
   - Update `CultureInfoManager.cs`
   - For MAUI: Update `MainActivity.cs`
   - Run translator tool

2. **Translation Workflow**:
   - Add English strings to `AppStrings.resx`
   - Run `Bit.ResxTranslator` tool
   - Review auto-translated strings
   - Adjust as needed

### IDE Configuration

1. **VS Code Setup**:
   - Install recommended extensions when prompted
   - Enable SCSS live compilation
   - Use workspace settings for consistency

2. **Visual Studio Setup**:
   - Import `settings.VisualStudio.json`
   - Enable Hot Reload features
   - Configure code style to match `.editorconfig`

---

## 11. Troubleshooting Common Issues

### Build Issues

**Problem:** CSS files causing merge conflicts
```
Solution: CSS files are generated from SCSS. Delete CSS files and regenerate:
1. Run Clean.bat/Clean.sh
2. Rebuild solution
```

**Problem:** "Project not found" errors with solution filter
```
Solution: Switch to full solution temporarily:
dotnet build Boilerplate.sln
```

**Problem:** MSBuild errors about missing SDKs
```
Solution: Ensure global.json SDK version is installed:
dotnet --list-sdks
# Install required SDK if missing
```

### IDE Issues

**Problem:** IntelliSense not working in VS Code
```
Solution:
1. Reload window: Ctrl+Shift+P → "Reload Window"
2. Clear OmniSharp cache: Delete .vscode/obj folder
3. Restart OmniSharp: Ctrl+Shift+P → "OmniSharp: Restart OmniSharp"
```

**Problem:** SCSS not compiling automatically
```
Solution:
1. Check extension is installed: "Live Sass Compiler"
2. Check .vscode/settings.json:
   "liveSassCompile.settings.watchOnLaunch": true
3. Restart VS Code
```

**Problem:** File nesting not working in VS Code
```
Solution: Check .vscode/settings.json:
"explorer.fileNesting.enabled": true
```

### Git Issues

**Problem:** Accidentally committed generated files
```
Solution:
git rm --cached path/to/file.css
git commit -m "Remove generated file from tracking"
```

**Problem:** Large `.vs` folder committed
```
Solution:
1. Add to .gitignore: .vs/
2. Remove from git:
   git rm -r --cached .vs
   git commit -m "Remove .vs folder"
```

### Localization Issues

**Problem:** ResxTranslator not working
```
Solution:
1. Check API key is set (environment variable or user secrets)
2. Verify internet connection
3. Check OpenAI/Azure endpoint is accessible
4. Review Bit.ResxTranslator.json configuration
```

**Problem:** Missing translations for new language
```
Solution:
1. Ensure language code in Bit.ResxTranslator.json
2. Run translator tool
3. Check output for errors
4. Manually create .resx file if needed: AppStrings.{lang}.resx
```

---

## 12. Customization Guide

### Adding Custom Build Properties

Edit `Directory.Build.props`:
```xml
<PropertyGroup>
    <!-- Your custom property -->
    <MyCustomProperty>CustomValue</MyCustomProperty>
</PropertyGroup>
```

Usage:
```bash
dotnet build -p:MyCustomProperty=AnotherValue
```

---

### Adding Custom Global Usings

Edit `Directory.Build.props`:
```xml
<ItemGroup>
    <Using Include="My.Custom.Namespace" />
</ItemGroup>
```

---

### Customizing Code Style

Edit `.editorconfig`:
```properties
[*.cs]
# Your custom rule
my_custom_rule = true:error
```

---

### Adding New Package

1. Add version to `Directory.Packages.props`:
```xml
<PackageVersion Include="NewPackage" Version="1.0.0" />
```

2. Reference in project (no version):
```xml
<PackageReference Include="NewPackage" />
```

---

### Creating Custom Cleanup Script

Create `CustomClean.bat`:
```bat
@echo off
echo Cleaning custom files...
del /s /q *.customext
rmdir /s /q CustomFolder
echo Done!
```

---

## Summary

These miscellaneous files form the foundation of your project's development experience:

- **Solution files** organize your projects
- **Build configuration files** control compilation behavior
- **Code quality files** ensure consistency
- **Version control files** manage what's tracked
- **IDE configuration files** optimize developer experience
- **Cleanup utilities** maintain a clean workspace
- **Documentation files** provide guidance
- **GitHub configuration** enables automation and AI assistance

Understanding and properly configuring these files will:
- Improve build performance
- Ensure code consistency
- Enhance collaboration
- Streamline workflows
- Reduce onboarding time
- Prevent common issues

**Next Steps:**
- Review and customize these files for your team's needs
- Ensure all team members use the same configurations
- Keep documentation up to date as project evolves
- Regularly update dependencies in `Directory.Packages.props`

---

**Ready to move forward? Let's proceed to Stage 20 (.NET Aspire) to learn about the modern application orchestration and service discovery features available in this template!**
