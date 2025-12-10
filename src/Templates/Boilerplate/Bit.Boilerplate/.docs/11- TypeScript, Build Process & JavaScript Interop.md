# Stage 11: TypeScript, Build Process & JavaScript Interop

Welcome to Stage 11! In this stage, you'll learn how the project integrates TypeScript with C# Blazor, the build process for compiling TypeScript and SCSS, and how to call JavaScript functions from C# code.

## Overview

This project uses **TypeScript** for type-safe JavaScript development, along with automated build processes that compile TypeScript to JavaScript and SCSS to CSS during the build pipeline. You'll also learn how to add new npm packages and call JavaScript functions from your C# Blazor components.

---

## 1. TypeScript Configuration

### Location
[`/src/Client/Boilerplate.Client.Core/tsconfig.json`](/src/Client/Boilerplate.Client.Core/tsconfig.json)

### Configuration

```jsonc
{
    "compileOnSave": true,
    "compilerOptions": {
        "strict": true,
        "target": "ES2019",
        "module": "es2015",
        // ...
    }
}
```

### Key Settings Explained

- **`strict: true`**: Enables all strict type-checking options for better code quality
- **`target: "ES2019"`**: Compiles TypeScript to ES2019 JavaScript
- **`module: "es2015"`**: Uses ES2015 module system (import/export)
- **`noImplicitAny: true`**: Requires explicit type annotations, preventing implicit `any` types
- **`lib: ["DOM", "ESNext"]`**: Includes DOM and modern JavaScript API type definitions
- **`moduleResolution: "node"`**: Uses Node.js-style module resolution

---

## 2. Package Management with npm

### Location
[`/src/Client/Boilerplate.Client.Core/package.json`](/src/Client/Boilerplate.Client.Core/package.json)

### Current Dependencies

```json
{
    "devDependencies": {
        "esbuild": "0.27.0",
        "sass": "1.94.0",
        "typescript": "5.9.3"
    }
}
```

### What Each Package Does

- **`typescript`**: The TypeScript compiler (`tsc`) that transforms `.ts` files to `.js`
- **`esbuild`**: Ultra-fast JavaScript bundler that combines all JavaScript modules into a single minified `app.js` file
- **`sass`**: SCSS/Sass compiler that transforms `.scss` files to `.css`

---

## 3. MSBuild Integration & Build Process

### Location
[`/src/Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj`](/src/Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj)

### Build Pipeline

The `.csproj` file defines custom MSBuild targets that run automatically during the build process:

```xml
<Target Name="BeforeBuildTasks" AfterTargets="CoreCompile">
    <CallTarget Targets="InstallNodejsDependencies" />
    <CallTarget Targets="BuildJavaScript" />
    <CallTarget Targets="BuildCssFiles" />
</Target>
```

### Build Process Flow

```
1. CoreCompile (C# compilation)
    ↓
2. BeforeBuildTasks
    ↓
3. InstallNodejsDependencies
    ↓
4. BuildJavaScript (TypeScript → JavaScript → Bundle)
    ↓
5. BuildCssFiles (SCSS → CSS)
```

### Step 1: InstallNodejsDependencies

```xml
<Target Name="InstallNodejsDependencies" Inputs="package.json" Outputs="node_modules\.package-lock.json">
    <Exec Command="npm install" StandardOutputImportance="high" StandardErrorImportance="high" />
</Target>
```

**What it does:**
- Runs `npm install` to install all packages from `package.json`
- Only runs when `package.json` changes (incremental build optimization)
- Creates `node_modules` folder with all dependencies

### Step 2: BuildJavaScript

```xml
<Target Name="BuildJavaScript" Inputs="@(TypeScriptFiles);tsconfig.json;package.json" Outputs="wwwroot\scripts\app.js">
    <Exec Command="node_modules/.bin/tsc" StandardOutputImportance="high" StandardErrorImportance="high" />
    <Exec Condition=" '$(Environment)' == 'Development' " 
          Command="node_modules/.bin/esbuild Scripts/index.js --bundle --outfile=wwwroot/scripts/app.js" 
          StandardOutputImportance="high" StandardErrorImportance="high" />
    <Exec Condition=" '$(Environment)' != 'Development' " 
          Command="node_modules/.bin/esbuild Scripts/index.js --bundle --outfile=wwwroot/scripts/app.js --minify" 
          StandardOutputImportance="high" StandardErrorImportance="high" />
</Target>
```

**What it does:**
1. **TypeScript Compilation**: Runs `tsc` (TypeScript compiler) to convert all `.ts` files to `.js` files
2. **Bundling (Development)**: Uses `esbuild` to bundle all JavaScript modules into a single `wwwroot/scripts/app.js` file
3. **Bundling + Minification (Production/Staging)**: Same as above, but with `--minify` flag for smaller file size

**Incremental Build Optimization:**
- Only rebuilds when TypeScript files, `tsconfig.json`, or `package.json` change
- Skips compilation if `app.js` is up-to-date

### Step 3: BuildCssFiles

```xml
<Target Name="BuildCssFiles">
    <Exec Command="node_modules/.bin/sass Components:Components Styles/app.scss:wwwroot/styles/app.css --style compressed --silence-deprecation=import --update --color" 
          StandardOutputImportance="high" StandardErrorImportance="high" LogStandardErrorAsError="true" />
</Target>
```

**What it does:**
- Compiles `Styles/app.scss` to `wwwroot/styles/app.css`
- Processes component-specific `.razor.scss` files in the `Components` folder
- Uses `--style compressed` for minified CSS output
- Uses `--update` flag to only overwrite changed files

---

## 4. JavaScript Interop: Calling JS from C#

### The JavaScript Side: App.ts

**Location:** [`/src/Client/Boilerplate.Client.Core/Scripts/App.ts`](/src/Client/Boilerplate.Client.Core/Scripts/App.ts)

This is the main TypeScript file that exposes JavaScript functions to C# code:

```typescript
export class App {
    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
}
```

### The C# Side: IJSRuntimeExtensions.cs

**Location:** [`/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs)

This file defines C# extension methods that call the JavaScript functions in `App.ts`:

```csharp
using System.Reflection;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Microsoft.JSInterop;

public static partial class IJSRuntimeExtensions
{
    public static ValueTask<string> GetTimeZone(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("App.getTimeZone");
    }
}
```

### Example: GetTimeZone Method

Let's focus on the `getTimeZone` method as a complete example:

#### JavaScript (App.ts)
```typescript
public static getTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
```

#### C# Extension Method (IJSRuntimeExtensions.cs)
```csharp
public static ValueTask<string> GetTimeZone(this IJSRuntime jsRuntime)
{
    return jsRuntime.InvokeAsync<string>("App.getTimeZone");
}
```

#### Usage in a Blazor Component
```csharp
@inject IJSRuntime JSRuntime

@code {
    private string? userTimeZone;

    protected override async Task OnAfterFirstRenderAsync()
    {
        userTimeZone = await JSRuntime.GetTimeZone();
        StateHasChanged();
    }
}
```

### How It Works

1. **TypeScript Method**: `App.getTimeZone()` uses the browser's `Intl.DateTimeFormat` API to get the user's timezone
2. **C# Extension Method**: `GetTimeZone()` calls `jsRuntime.InvokeAsync<string>("App.getTimeZone")` to invoke the JavaScript function
3. **Component Usage**: Any Blazor component can call `await JSRuntime.GetTimeZone()` to get the user's timezone

---

## 5. Demo: Adding a New npm Package (uuid)

Let's walk through a complete example of adding the `uuid` package to generate unique identifiers.

### Step 1: Install the Package

Run the following commands in the `Boilerplate.Client.Core` directory:

```powershell
cd src/Client/Boilerplate.Client.Core
npm install uuid
npm install --save-dev @types/uuid
```

**What each command does:**
- `npm install uuid`: Installs the `uuid` package (runtime dependency)
- `npm install --save-dev @types/uuid`: Installs TypeScript type definitions for `uuid` (development dependency)

### Step 2: Update package.json

After running the commands, your `package.json` should look like this:

```json
{
    "dependencies": {
        "uuid": "^11.0.3"
    },
    "devDependencies": {
        "esbuild": "0.27.0",
        "sass": "1.94.0",
        "typescript": "5.9.3",
        "@types/uuid": "^10.0.0"
    }
}
```

### Step 3: Add TypeScript Method in App.ts

Add this import at the top and new method to the `App` class in [`Scripts/App.ts`](/src/Client/Boilerplate.Client.Core/Scripts/App.ts):

```typescript
import { v4 as uuidv4 } from 'uuid';

export class App {
    // ... existing methods ...

    public static generateUuid(): string {
        return uuidv4();
    }
}
```

### Step 4: Add C# Extension Method

Add this method to [`Extensions/IJSRuntimeExtensions.cs`](/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs):

```csharp
public static ValueTask<string> GenerateUuid(this IJSRuntime jsRuntime)
{
    return jsRuntime.InvokeAsync<string>("App.generateUuid");
}
```

### Step 5: Use in a Blazor Component

Now you can use it in any Blazor component:

```xml
@page "/uuid-demo"
@inject IJSRuntime JSRuntime

<BitText Typography="BitTypography.H4">UUID Generator Demo</BitText>

<BitButton OnClick="GenerateNewUuid">Generate UUID</BitButton>

@if (!string.IsNullOrEmpty(generatedUuid))
{
    <BitText>Generated UUID: @generatedUuid</BitText>
}

@code {
    private string? generatedUuid;

    private async Task GenerateNewUuid()
    {
        generatedUuid = await JSRuntime.GenerateUuid();
    }
}
```

### Step 6: Build the Project

Run the build to compile TypeScript and bundle the new code:

```powershell
cd src/Server/Boilerplate.Server.Web
dotnet build
```

The build process will:
1. Install the new `uuid` package (via `npm install`)
2. Compile `App.ts` to JavaScript (via `tsc`)
3. Bundle all JavaScript including `uuid` into `app.js` (via `esbuild`)

---

## 6. Common Scenarios

### Adding a New TypeScript File

1. Create the file in `Scripts/` folder (e.g., `Scripts/MyHelpers.ts`)
2. Export functions you want to use:
   ```typescript
   export function myHelper(): string {
       return "Hello from TypeScript!";
   }
   ```
3. Import in `Scripts/index.ts` and expose on window if needed:
   ```typescript
   import { myHelper } from './MyHelpers';
   (window as any).myHelper = myHelper;
   ```
4. Build the project - TypeScript compiler and esbuild will handle it automatically

---